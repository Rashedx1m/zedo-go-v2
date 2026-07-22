using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<DriverLocation> DriverLocations => Set<DriverLocation>();
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<RidePricing> RidePricings => Set<RidePricing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(20).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Phone).IsUnique();
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Rating).HasPrecision(3, 2);
            entity.HasOne(e => e.User).WithOne(u => u.Customer)
                  .HasForeignKey<Customer>(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CarModel).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CarColor).HasMaxLength(30).IsRequired();
            entity.Property(e => e.PlateNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.LicenseNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Rating).HasPrecision(3, 2);
            entity.Property(e => e.TotalEarnings).HasPrecision(18, 2);
            entity.HasOne(e => e.User).WithOne(u => u.Driver)
                  .HasForeignKey<Driver>(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.PlateNumber).IsUnique();
        });

        modelBuilder.Entity<DriverLocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Driver).WithOne(d => d.CurrentLocation)
                  .HasForeignKey<DriverLocation>(e => e.DriverId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.DriverId).IsUnique();
        });

        modelBuilder.Entity<Request>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EstimatedCost).HasPrecision(18, 2);
            entity.Property(e => e.ActualCost).HasPrecision(18, 2);
            entity.HasOne(e => e.Customer).WithMany(c => c.Requests)
                  .HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Driver).WithMany(d => d.Requests)
                  .HasForeignKey(e => e.DriverId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.CompanyCommission).HasPrecision(18, 2);
            entity.Property(e => e.DriverEarning).HasPrecision(18, 2);

            // Request
            entity.HasOne(e => e.Request)
                  .WithOne(r => r.Payment)
                  .HasForeignKey<Payment>(e => e.RequestId)
                  .OnDelete(DeleteBehavior.NoAction);

            // Customer
            entity.HasOne(e => e.Customer)
                  .WithMany()
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.NoAction);

            // Driver
            entity.HasOne(e => e.Driver)
                  .WithMany()
                  .HasForeignKey(e => e.DriverId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(e => e.RequestId).IsUnique();
        });

        modelBuilder.Entity<RidePricing>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.BaseFare).HasPrecision(18, 2);
            entity.Property(e => e.PricePerKm).HasPrecision(18, 2);
            entity.Property(e => e.PricePerMinute).HasPrecision(18, 2);
            entity.Property(e => e.MinimumFare).HasPrecision(18, 2);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
            entry.Entity.UpdatedAt = DateTime.UtcNow;

        return base.SaveChangesAsync(cancellationToken);
    }
}
