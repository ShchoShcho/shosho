using Microsoft.EntityFrameworkCore;
using ShoSho.Core.Models;

namespace ShoSho.Infrastructure.Data;

public class ShoShoDbContext : DbContext
{
    public const string DefaultConnectionString = "Host=localhost;Port=5432;Database=shosho_db;Username=postgres;Password=postgres";

    public DbSet<MediaItem> MediaItems { get; set; } = null!;

    public ShoShoDbContext()
    {
    }

    public ShoShoDbContext(DbContextOptions<ShoShoDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql(DefaultConnectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MediaItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PosterUrl).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone");
        });
    }
}

