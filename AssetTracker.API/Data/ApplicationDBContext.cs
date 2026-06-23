using Microsoft.EntityFrameworkCore;
using AssetTracker.API.Models;

namespace AssetTracker.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<AuditTrail> AuditTrails { get; set; }
        public DbSet<Checkout> Checkouts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map models to explicit singular/plural table names as generated in SQL Server
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Asset>().ToTable("Assets");
            modelBuilder.Entity<Checkout>().ToTable("Checkouts");
            modelBuilder.Entity<AuditTrail>().ToTable("AuditTrail");

            // Configure decimal precision for Asset.Value to prevent silent truncation
            modelBuilder.Entity<Asset>()
                .Property(a => a.Value)
                .HasPrecision(18, 2);

            // Database-generated creation timestamp; also backfills existing rows on migration.
            modelBuilder.Entity<Asset>()
                .Property(a => a.DateAdded)
                .HasDefaultValueSql("GETUTCDATE()");

            // Enforce unique constraints at the database level (brief 3.1 & 3.2)
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.SerialNumber)
                .IsUnique();
        }
    }
}