using Microsoft.EntityFrameworkCore;
using TimeBank.API.Models;

namespace TimeBank.API.Data;

public class TimeBankDbContext : DbContext
{
    public TimeBankDbContext(DbContextOptions<TimeBankDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<ServiceRequest> ServiceRequests { get; set; }
    public DbSet<TimeTransaction> TimeTransactions { get; set; }
    public DbSet<Review> Reviews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // El correo no se puede repetir
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        // Horas con 2 decimales (ej. 1.50 horas)
        modelBuilder.Entity<User>().Property(u => u.HoursBalance).HasPrecision(6, 2);
        modelBuilder.Entity<Service>().Property(s => s.EstimatedHours).HasPrecision(6, 2);
        modelBuilder.Entity<ServiceRequest>().Property(r => r.RequestedHours).HasPrecision(6, 2);
        modelBuilder.Entity<TimeTransaction>().Property(t => t.Hours).HasPrecision(6, 2);

        // Un usuario ofrece muchos servicios
        modelBuilder.Entity<Service>()
            .HasOne(s => s.User).WithMany(u => u.Services)
            .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);

        // Un usuario hace muchas solicitudes
        modelBuilder.Entity<ServiceRequest>()
            .HasOne(r => r.Requester).WithMany(u => u.Requests)
            .HasForeignKey(r => r.RequesterId).OnDelete(DeleteBehavior.Restrict);

        // Cada solicitud completada genera una sola transacción de horas
        modelBuilder.Entity<TimeTransaction>()
            .HasOne(t => t.ServiceRequest).WithOne(r => r.Transaction)
            .HasForeignKey<TimeTransaction>(t => t.ServiceRequestId);

        // La transacción y la valoración apuntan dos veces a User, por eso se configuran a mano
        modelBuilder.Entity<TimeTransaction>()
            .HasOne(t => t.FromUser).WithMany()
            .HasForeignKey(t => t.FromUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TimeTransaction>()
            .HasOne(t => t.ToUser).WithMany()
            .HasForeignKey(t => t.ToUserId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Reviewer).WithMany()
            .HasForeignKey(r => r.ReviewerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Review>()
            .HasOne(r => r.ReviewedUser).WithMany()
            .HasForeignKey(r => r.ReviewedUserId).OnDelete(DeleteBehavior.Restrict);

        // Categorías iniciales, para que el SELECT de prueba devuelva datos
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Tutorías y clases", Description = "Refuerzo académico, idiomas, música" },
            new Category { Id = 2, Name = "Tecnología", Description = "Soporte de computadoras, programación, redes" },
            new Category { Id = 3, Name = "Hogar", Description = "Reparaciones, jardinería, limpieza" },
            new Category { Id = 4, Name = "Cuidado de personas", Description = "Acompañamiento, cuidado de niños o adultos mayores" },
            new Category { Id = 5, Name = "Transporte y mandados", Description = "Traslados, compras, trámites" }
        );
    }
}
