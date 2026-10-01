using System.ComponentModel.DataAnnotations;

namespace TimeBank.API.Dtos;

// ===== Datos que ENTRAN a /api/auth =====

public class RegisterDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(60, ErrorMessage = "El nombre no puede pasar de 60 caracteres")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [StringLength(60, ErrorMessage = "El apellido no puede pasar de 60 caracteres")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string Password { get; set; } = string.Empty;

    [Phone(ErrorMessage = "El teléfono no tiene un formato válido")]
    public string? Phone { get; set; }
}

public class LoginDto
{
    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(60, ErrorMessage = "El nombre no puede pasar de 60 caracteres")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [StringLength(60, ErrorMessage = "El apellido no puede pasar de 60 caracteres")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "El teléfono no tiene un formato válido")]
    public string? Phone { get; set; }
}

public class ChangePasswordDto
{
    [Required(ErrorMessage = "La contraseña actual es obligatoria")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
    [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres")]
    public string NewPassword { get; set; } = string.Empty;
}

// ===== Datos que SALEN de /api/auth =====

// Perfil del usuario sin datos sensibles (nunca incluye la contraseña)
public record UserProfileDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    decimal HoursBalance,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

// Respuesta del login: el token que el frontend o Postman deben enviar en cada petición protegida
public record AuthResponseDto(
    string Token,
    string TokenType,
    DateTime ExpiresAt,
    UserProfileDto User);

public record SessionDto(
    int Id,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? RevokedAt,
    bool IsActive,
    bool IsCurrent,
    string? IpAddress,
    string? UserAgent);
