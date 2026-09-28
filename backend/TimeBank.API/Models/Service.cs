namespace TimeBank.API.Models;

// Servicio que un usuario ofrece a cambio de horas
public class Service
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal EstimatedHours { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<ServiceRequest> Requests { get; set; } = new List<ServiceRequest>();
}
