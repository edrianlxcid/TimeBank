namespace TimeBank.API.Models;

// Categoría del servicio: tutorías, tecnología, hogar, etc.
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Service> Services { get; set; } = new List<Service>();
}
