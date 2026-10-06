using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeBank.API.Data;
using TimeBank.API.Dtos;
using TimeBank.API.Models;
using TimeBank.API.Security;

namespace TimeBank.API.Services;

// Lógica del login: registro, inicio y cierre de sesión, perfil y contraseña.
// Aquí están las reglas; el controlador solo recibe la petición y devuelve la respuesta.
public class AuthService : IAuthService
{
    // Igual que en UsersController: cada usuario nuevo recibe 2 horas de bienvenida
    private const decimal WelcomeHours = 2;

    // Mismo mensaje para correo inexistente o contraseña incorrecta,
    // para no revelar qué correos están registrados
    private const string InvalidCredentials = "Correo o contraseña incorrectos";

    private readonly TimeBankDbContext _context;
    private readonly IPasswordHasher<User> _hasher;
    private readonly ITokenService _tokenService;

    public AuthService(TimeBankDbContext context, IPasswordHasher<User> hasher, ITokenService tokenService)
    {
        _context = context;
        _hasher = hasher;
        _tokenService = tokenService;
    }

    public async Task<ServiceResult<UserProfileDto>> RegisterAsync(RegisterDto dto)
    {
        var email = NormalizeEmail(dto.Email);
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
        {
            return ServiceResult<UserProfileDto>.Fail(ServiceErrorType.Conflict, "Ya existe un usuario con ese correo");
        }

        var user = new User
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = email,
            Phone = dto.Phone,
            HoursBalance = WelcomeHours,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        // Quien se registra solo puede ser "Usuario". Los administradores los crea otro administrador.
        var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == AppRoles.User);
        if (userRole == null)
        {
            return ServiceResult<UserProfileDto>.Fail(ServiceErrorType.Validation,
                "No existe el rol Usuario. Ejecuta 'dotnet ef database update'");
        }
        user.UserRoles.Add(new UserRole { Role = userRole, AssignedAt = DateTime.UtcNow });

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return ServiceResult<UserProfileDto>.Ok(ToProfile(user));
    }

    public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto dto, string? ipAddress, string? userAgent)
    {
        var email = NormalizeEmail(dto.Email);
        var user = await UsersWithRoles().FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (user == null)
        {
            return ServiceResult<AuthResponseDto>.Fail(ServiceErrorType.Unauthorized, InvalidCredentials);
        }

        var check = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (check == PasswordVerificationResult.Failed)
        {
            return ServiceResult<AuthResponseDto>.Fail(ServiceErrorType.Unauthorized, InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return ServiceResult<AuthResponseDto>.Fail(ServiceErrorType.Forbidden, "El usuario está desactivado");
        }

        // Si el algoritmo de encriptación se actualizó, se vuelve a guardar la contraseña con el nuevo
        if (check == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, dto.Password);
        }

        // Los roles viajan dentro del token para que la API sepa qué puede hacer el usuario
        var (token, tokenId, expiresAt) = _tokenService.CreateToken(user, RoleNames(user));

        _context.UserSessions.Add(new UserSession
        {
            UserId = user.Id,
            TokenId = tokenId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IpAddress = Truncate(ipAddress, 64),
            UserAgent = Truncate(userAgent, 256)
        });
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ServiceResult<AuthResponseDto>.Ok(new AuthResponseDto(token, "Bearer", expiresAt, ToProfile(user)));
    }

    public async Task<ServiceResult<UserProfileDto>> GetProfileAsync(int userId)
    {
        var user = await UsersWithRoles().FirstOrDefaultAsync(u => u.Id == userId);
        return user == null
            ? ServiceResult<UserProfileDto>.Fail(ServiceErrorType.NotFound, "Usuario no encontrado")
            : ServiceResult<UserProfileDto>.Ok(ToProfile(user));
    }

    public async Task<ServiceResult<UserProfileDto>> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await UsersWithRoles().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return ServiceResult<UserProfileDto>.Fail(ServiceErrorType.NotFound, "Usuario no encontrado");
        }

        // El correo, el saldo y la contraseña no se cambian aquí
        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.Phone = dto.Phone;
        await _context.SaveChangesAsync();

        return ServiceResult<UserProfileDto>.Ok(ToProfile(user));
    }

    public async Task<ServiceResult<string>> ChangePasswordAsync(int userId, string currentTokenId, ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return ServiceResult<string>.Fail(ServiceErrorType.NotFound, "Usuario no encontrado");
        }

        if (_hasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            return ServiceResult<string>.Fail(ServiceErrorType.Validation, "La contraseña actual no es correcta");
        }

        if (dto.CurrentPassword == dto.NewPassword)
        {
            return ServiceResult<string>.Fail(ServiceErrorType.Validation, "La nueva contraseña debe ser diferente a la actual");
        }

        user.PasswordHash = _hasher.HashPassword(user, dto.NewPassword);

        // Por seguridad se cierran las demás sesiones abiertas; la actual sigue activa
        var now = DateTime.UtcNow;
        var otherSessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.TokenId != currentTokenId && s.RevokedAt == null && s.ExpiresAt > now)
            .ToListAsync();
        otherSessions.ForEach(s => s.RevokedAt = now);

        await _context.SaveChangesAsync();
        var closed = otherSessions.Count == 1 ? "Se cerró 1 sesión" : $"Se cerraron {otherSessions.Count} sesiones";
        return ServiceResult<string>.Ok($"Contraseña actualizada. {closed} en otros dispositivos");
    }

    public async Task<ServiceResult<IReadOnlyList<SessionDto>>> GetSessionsAsync(int userId, string currentTokenId)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        IReadOnlyList<SessionDto> result = sessions
            .Select(s => new SessionDto(s.Id, s.CreatedAt, s.ExpiresAt, s.RevokedAt, s.IsActive,
                s.TokenId == currentTokenId, s.IpAddress, s.UserAgent))
            .ToList();

        return ServiceResult<IReadOnlyList<SessionDto>>.Ok(result);
    }

    public async Task<ServiceResult<bool>> LogoutAsync(string tokenId)
    {
        var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.TokenId == tokenId);
        if (session == null)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.NotFound, "Sesión no encontrada");
        }

        session.RevokedAt ??= DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> RevokeSessionAsync(int userId, int sessionId)
    {
        // Solo se pueden cerrar sesiones propias
        var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
        if (session == null)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.NotFound, "Sesión no encontrada");
        }

        if (session.RevokedAt != null)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.Validation, "Esa sesión ya estaba cerrada");
        }

        session.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ServiceResult<bool>.Ok(true);
    }

    // Se usa en cada petición con token: si la sesión se cerró, el token ya no sirve
    public async Task<bool> IsSessionActiveAsync(string tokenId)
    {
        var now = DateTime.UtcNow;
        return await _context.UserSessions.AnyAsync(s =>
            s.TokenId == tokenId && s.RevokedAt == null && s.ExpiresAt > now && s.User.IsActive);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string? Truncate(string? value, int max) =>
        value == null || value.Length <= max ? value : value[..max];

    // Usuarios con sus roles cargados (Include = JOIN con UserRoles y Roles)
    private IQueryable<User> UsersWithRoles() =>
        _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

    private static List<string> RoleNames(User u) =>
        u.UserRoles.Select(ur => ur.Role.Name).OrderBy(n => n).ToList();

    private static UserProfileDto ToProfile(User u) =>
        new(u.Id, u.FirstName, u.LastName, u.Email, u.Phone, u.HoursBalance, u.IsActive, u.CreatedAt, u.LastLoginAt,
            RoleNames(u));
}
