using Microsoft.EntityFrameworkCore;
using TimeBank.API.Models;
using TimeBank.API.Security;

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
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

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

        // Sesiones de login: un usuario tiene muchas sesiones; si se borra el usuario, se borran sus sesiones
        modelBuilder.Entity<UserSession>(session =>
        {
            session.HasOne(s => s.User).WithMany(u => u.Sessions)
                .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
            session.HasIndex(s => s.TokenId).IsUnique(); // cada token identifica una sola sesión
            session.Property(s => s.TokenId).HasMaxLength(64);
            session.Property(s => s.IpAddress).HasMaxLength(64);
            session.Property(s => s.UserAgent).HasMaxLength(256);
            session.Ignore(s => s.IsActive); // se calcula, no es una columna
        });

        // Roles: nombre único y relación muchos a muchos con usuarios mediante UserRoles
        modelBuilder.Entity<Role>(role =>
        {
            role.HasIndex(r => r.Name).IsUnique();
            role.Property(r => r.Name).HasMaxLength(50);
            role.HasData(
                new Role { Id = 1, Name = AppRoles.Admin, Description = "Administra categorías, usuarios y roles" },
                new Role { Id = 2, Name = AppRoles.User, Description = "Ofrece y solicita servicios con su saldo de horas" });
        });

        modelBuilder.Entity<UserRole>(userRole =>
        {
            userRole.HasKey(ur => new { ur.UserId, ur.RoleId }); // clave compuesta: un rol una sola vez por usuario
            userRole.HasOne(ur => ur.User).WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
            userRole.HasOne(ur => ur.Role).WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

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
