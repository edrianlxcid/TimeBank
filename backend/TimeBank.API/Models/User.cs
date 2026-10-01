using System.Text.Json.Serialization;

namespace TimeBank.API.Models;

// Usuario de la plataforma: ofrece servicios y pide servicios a otros
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [JsonIgnore] // nunca se devuelve en las respuestas de la API
    public string PasswordHash { get; set; } = string.Empty; // la contraseña se guarda encriptada
    public string? Phone { get; set; }
    public decimal HoursBalance { get; set; } // saldo de horas del banco de tiempo
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; } // último inicio de sesión (se llena en el login)

    public ICollection<Service> Services { get; set; } = new List<Service>();
    public ICollection<ServiceRequest> Requests { get; set; } = new List<ServiceRequest>();

    [JsonIgnore] // las sesiones se consultan aparte en /api/auth/sessions
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}
