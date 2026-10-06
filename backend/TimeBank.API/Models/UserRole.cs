namespace TimeBank.API.Models;

// Tabla intermedia usuario-rol (relación muchos a muchos):
// un usuario puede tener varios roles y un rol lo tienen muchos usuarios
public class UserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
