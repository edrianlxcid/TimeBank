using System.Text.Json.Serialization;

namespace TimeBank.API.Models;

// Rol del sistema: Administrador o Usuario (ver Security/AppRoles.cs)
public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    [JsonIgnore]
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
