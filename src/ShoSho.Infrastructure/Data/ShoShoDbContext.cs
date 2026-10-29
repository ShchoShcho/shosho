using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using ShoSho.Core.Models;
using System;
using System.IO;

namespace ShoSho.Infrastructure.Data;

public class ShoShoDbContext : DbContext
{
    public const string DefaultConnectionString = "Data Source=shosho.db";

    public DbSet<MediaItem> MediaItems { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;

    public DbSet<Review> Reviews { get; set; } = null!;

    public ShoShoDbContext() { }

    public ShoShoDbContext(DbContextOptions<ShoShoDbContext> options) : base(options) { }

    public static void Configure(DbContextOptionsBuilder optionsBuilder, string connectionString)
    {
        var connection = new SqliteConnectionStringBuilder(connectionString);
        if (connection.DataSource != ":memory:" && connection.Mode != SqliteOpenMode.Memory)
        {
            if (string.IsNullOrWhiteSpace(connection.DataSource))
                throw new ArgumentException("SQLite database file must be specified.", nameof(connectionString));

            var dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShoSho");
            connection.DataSource = Path.GetFullPath(connection.DataSource, dataDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(connection.DataSource)!);
        }

        optionsBuilder.UseSqlite(connection.ToString());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            Configure(optionsBuilder, DefaultConnectionString);
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

            entity.Property(e => e.CreatedAt).HasConversion(
                value => value.ToUniversalTime(),
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<MediaItem>()
            .HasMany(m => m.Tags)
            .WithMany(t => t.MediaItems)
            .UsingEntity(j => j.ToTable("ItemTag"));

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).HasMaxLength(2000);
            entity.Property(e => e.Rating).IsRequired(false);
        });

        modelBuilder.Entity<MediaItem>()
            .HasOne(m => m.Review)
            .WithOne(r => r.MediaItem)
            .HasForeignKey<Review>(r => r.MediaItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}