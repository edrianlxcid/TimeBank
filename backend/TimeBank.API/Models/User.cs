namespace TimeBank.API.Models;

// Usuario de la plataforma: ofrece servicios y pide servicios a otros
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // la contraseña se guarda encriptada
    public string? Phone { get; set; }
    public decimal HoursBalance { get; set; } // saldo de horas del banco de tiempo
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Service> Services { get; set; } = new List<Service>();
    public ICollection<ServiceRequest> Requests { get; set; } = new List<ServiceRequest>();
}
