using System.ComponentModel.DataAnnotations;
using TimeBank.API.Security;

namespace TimeBank.API.Dtos;

// DTOs: definen qué datos entran a la API, sin exponer todo el modelo

public class CreateUserDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [StrongPassword]
    public string Password { get; set; } = string.Empty;

    public string? Phone { get; set; }

    // Solo el administrador crea usuarios por aquí y elige sus roles.
    // Si no se envía, el usuario queda con el rol "Usuario".
    public List<string>? Roles { get; set; }
}

public class AssignRolesDto
{
    [Required(ErrorMessage = "Debes enviar al menos un rol")]
    [MinLength(1, ErrorMessage = "Debes enviar al menos un rol")]
    public List<string> Roles { get; set; } = new();
}

public class UpdateUserDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido")]
    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ServiceDto
{
    [Required(ErrorMessage = "El usuario que ofrece el servicio es obligatorio")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "La categoría es obligatoria")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "El título es obligatorio")]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Range(0.5, 24, ErrorMessage = "Las horas estimadas deben estar entre 0.5 y 24")]
    public decimal EstimatedHours { get; set; }

    public bool IsActive { get; set; } = true;
}

public class CreateServiceRequestDto
{
    public int ServiceId { get; set; }
    public int RequesterId { get; set; }
    public string? Message { get; set; }

    [Range(0.5, 24, ErrorMessage = "Las horas solicitadas deben estar entre 0.5 y 24")]
    public decimal RequestedHours { get; set; }
}

public class ChangeStatusDto
{
    [Required(ErrorMessage = "El estado es obligatorio")]
    public string Status { get; set; } = string.Empty; // Aceptada, Rechazada o Completada
}

public class CreateReviewDto
{
    public int ServiceRequestId { get; set; }
    public int ReviewerId { get; set; }

    [Range(1, 5, ErrorMessage = "La valoración debe ser de 1 a 5")]
    public int Rating { get; set; }

    public string? Comment { get; set; }
}
