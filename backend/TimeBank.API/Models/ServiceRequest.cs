namespace TimeBank.API.Models;

// Solicitud que hace un usuario para recibir un servicio
public class ServiceRequest
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public int RequesterId { get; set; }
    public string? Message { get; set; }
    public decimal RequestedHours { get; set; }
    public string Status { get; set; } = "Pendiente"; // Pendiente, Aceptada, Rechazada, Completada
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Service Service { get; set; } = null!;
    public User Requester { get; set; } = null!;
    public TimeTransaction? Transaction { get; set; }
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
