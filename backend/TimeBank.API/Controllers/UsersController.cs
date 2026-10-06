using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeBank.API.Data;
using TimeBank.API.Dtos;
using TimeBank.API.Models;
using TimeBank.API.Security;

namespace TimeBank.API.Controllers;

// Administración de usuarios. La gestión (listar, crear, editar, roles, eliminar) es solo del Administrador.
// Cualquier usuario con sesión puede ver el perfil de otro (para saber quién ofrece un servicio).
// Para registrarse por su cuenta, el usuario usa POST /api/auth/register (queda con el rol Usuario).
[Route("api/[controller]")] // ruta: /api/users
public class UsersController : ApiControllerBase
{
    // Horas de bienvenida que recibe cada usuario nuevo para poder empezar a intercambiar
    private const decimal WelcomeHours = 2;

    private readonly TimeBankDbContext _context;
    private readonly IPasswordHasher<User> _hasher;

    public UsersController(TimeBankDbContext context, IPasswordHasher<User> hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    // GET: api/users?search=ana
    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<IEnumerable<UserProfileDto>>> GetUsers([FromQuery] string? search)
    {
        var query = UsersWithRoles();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.ToLower();
            query = query.Where(u => u.FirstName.ToLower().Contains(text)
                                  || u.LastName.ToLower().Contains(text)
                                  || u.Email.ToLower().Contains(text));
        }

        var users = await query.OrderBy(u => u.LastName).ToListAsync();
        return Ok(users.Select(ToProfile));
    }

    // GET: api/users/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserProfileDto>> GetUser(int id)
    {
        var user = await UsersWithRoles().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado" });
        }
        return Ok(ToProfile(user));
    }

    // POST: api/users  -> el Administrador crea usuarios y elige su rol (por ejemplo otro Administrador)
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<UserProfileDto>> CreateUser(CreateUserDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var emailInUse = await _context.Users.AnyAsync(u => u.Email.ToLower() == email);
        if (emailInUse)
        {
            return BadRequest(new { message = "Ya existe un usuario con ese correo" });
        }

        var (roles, roleError) = await FindRolesAsync(dto.Roles is { Count: > 0 } ? dto.Roles : [AppRoles.User]);
        if (roleError != null) return BadRequest(new { message = roleError });

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = email,
            Phone = dto.Phone,
            HoursBalance = WelcomeHours,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password); // la contraseña se guarda encriptada
        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole { Role = role, AssignedAt = DateTime.UtcNow });
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, ToProfile(user));
    }

    // PUT: api/users/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado" });
        }

        var emailInUse = await _context.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == dto.Email.ToLower());
        if (emailInUse)
        {
            return BadRequest(new { message = "Ya existe otro usuario con ese correo" });
        }

        if (id == CurrentUserId && !dto.IsActive)
        {
            return BadRequest(new { message = "No puedes desactivar tu propia cuenta de administrador" });
        }

        // El saldo de horas NO se edita aquí: solo cambia con los intercambios
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Email = dto.Email;
        user.Phone = dto.Phone;
        user.IsActive = dto.IsActive;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PUT: api/users/5/roles  -> cambia los roles de un usuario (reemplaza los que tenía)
    [HttpPut("{id:int}/roles")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<UserProfileDto>> UpdateRoles(int id, AssignRolesDto dto)
    {
        var user = await UsersWithRoles().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado" });
        }

        var (roles, roleError) = await FindRolesAsync(dto.Roles);
        if (roleError != null) return BadRequest(new { message = roleError });

        // Evita que el administrador se quite a sí mismo el permiso y quede sin acceso
        if (id == CurrentUserId && roles.All(r => r.Name != AppRoles.Admin))
        {
            return BadRequest(new { message = "No puedes quitarte tu propio rol de Administrador" });
        }

        user.UserRoles.Clear();
        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole { UserId = user.Id, Role = role, AssignedAt = DateTime.UtcNow });
        }
        await _context.SaveChangesAsync();

        return Ok(ToProfile(user));
    }

    // DELETE: api/users/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (id == CurrentUserId)
        {
            return BadRequest(new { message = "No puedes eliminar tu propia cuenta" });
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado" });
        }

        // Regla: si ya participó en servicios o solicitudes, se desactiva en lugar de borrarse,
        // para no perder el historial de horas
        var hasHistory = await _context.Services.AnyAsync(s => s.UserId == id)
                      || await _context.ServiceRequests.AnyAsync(r => r.RequesterId == id);
        if (hasHistory)
        {
            user.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "El usuario tiene historial, así que se desactivó en lugar de eliminarse" });
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Busca los roles por nombre (sin importar mayúsculas) y avisa si alguno no existe
    private async Task<(List<Role> Roles, string? Error)> FindRolesAsync(IEnumerable<string> names)
    {
        var wanted = names.Select(n => n.Trim().ToLower()).Distinct().ToList();
        var roles = await _context.Roles.Where(r => wanted.Contains(r.Name.ToLower())).ToListAsync();
        if (roles.Count != wanted.Count)
        {
            return (roles, $"Rol no válido. Los roles disponibles son: {string.Join(", ", AppRoles.All)}");
        }
        return (roles, null);
    }

    private IQueryable<User> UsersWithRoles() =>
        _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

    private static UserProfileDto ToProfile(User u) =>
        new(u.Id, u.FirstName, u.LastName, u.Email, u.Phone, u.HoursBalance, u.IsActive, u.CreatedAt, u.LastLoginAt,
            u.UserRoles.Select(ur => ur.Role.Name).OrderBy(n => n).ToList());
}
