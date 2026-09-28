namespace TimeBank.API.Models;

// Valoración que deja un usuario después de un intercambio
public class Review
{
    public int Id { get; set; }
    public int ServiceRequestId { get; set; }
    public int ReviewerId { get; set; }
    public int ReviewedUserId { get; set; }
    public int Rating { get; set; } // de 1 a 5
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ServiceRequest ServiceRequest { get; set; } = null!;
    public User Reviewer { get; set; } = null!;
    public User ReviewedUser { get; set; } = null!;
}
