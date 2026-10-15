using Microsoft.EntityFrameworkCore;
using NuxibaApi.Models;

namespace NuxibaApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Login> Logins => Set<Login>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Area> Areas => Set<Area>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ccloglogin: la migración crea la tabla con un Id identity como PK.
        modelBuilder.Entity<Login>(e =>
        {
            e.ToTable("ccloglogin");
            e.HasKey(l => l.Id);
            e.Property(l => l.fecha).HasColumnType("datetime");

            // Índice para acelerar las búsquedas por usuario y fecha
            // (validación del último movimiento y cálculo de sesiones).
            e.HasIndex(l => new { l.User_id, l.fecha });
        });

        // ccUsers: el User_id viene en los datos, no es autogenerado.
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("ccUsers");
            e.HasKey(u => u.User_id);
            e.Property(u => u.User_id).ValueGeneratedNever();
            e.Property(u => u.Login).HasMaxLength(100).IsRequired();
            e.Property(u => u.Nombres).HasMaxLength(100);
            e.Property(u => u.ApellidoPaterno).HasMaxLength(100);
            e.Property(u => u.ApellidoMaterno).HasMaxLength(100);
        });

        // ccRIACat_Areas: el IDArea también viene del catálogo.
        modelBuilder.Entity<Area>(e =>
        {
            e.ToTable("ccRIACat_Areas");
            e.HasKey(a => a.IDArea);
            e.Property(a => a.IDArea).ValueGeneratedNever();
            e.Property(a => a.AreaName).HasMaxLength(100).IsRequired();
        });
    }
}
