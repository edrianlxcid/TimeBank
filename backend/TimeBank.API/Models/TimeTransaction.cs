namespace TimeBank.API.Models;

// Movimiento de horas: quien recibe el servicio paga horas a quien lo presta
public class TimeTransaction
{
    public int Id { get; set; }
    public int ServiceRequestId { get; set; }
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public decimal Hours { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ServiceRequest ServiceRequest { get; set; } = null!;
    public User FromUser { get; set; } = null!;
    public User ToUser { get; set; } = null!;
}
