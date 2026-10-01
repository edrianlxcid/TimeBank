namespace TimeBank.API.Models;

// Sesión de inicio de sesión: se crea una por cada login exitoso.
// Permite cerrar sesión de verdad (revocar el token) y ver desde dónde se conectó el usuario.
public class UserSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TokenId { get; set; } = string.Empty; // "jti" del token JWT, identifica la sesión
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; } // tiene fecha cuando se cerró la sesión (logout)
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; } // navegador o programa desde el que se hizo login (ej. Postman)

    // La sesión está activa si no se cerró y el token todavía no vence
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;

    public User User { get; set; } = null!;
}
