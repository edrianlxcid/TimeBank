using TimeBank.API.Dtos;

namespace TimeBank.API.Services;

// Contrato del módulo de autenticación: el controlador solo conoce esta interfaz
public interface IAuthService
{
    Task<ServiceResult<UserProfileDto>> RegisterAsync(RegisterDto dto);
    Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto dto, string? ipAddress, string? userAgent);
    Task<ServiceResult<UserProfileDto>> GetProfileAsync(int userId);
    Task<ServiceResult<UserProfileDto>> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task<ServiceResult<string>> ChangePasswordAsync(int userId, string currentTokenId, ChangePasswordDto dto);
    Task<ServiceResult<IReadOnlyList<SessionDto>>> GetSessionsAsync(int userId, string currentTokenId);
    Task<ServiceResult<bool>> LogoutAsync(string tokenId);
    Task<ServiceResult<bool>> RevokeSessionAsync(int userId, int sessionId);
    Task<bool> IsSessionActiveAsync(string tokenId);
}
