using Microsoft.EntityFrameworkCore;
using RepairDesk.Api.Models;

namespace RepairDesk.Api.Data;

public class RepairDeskDbContext : DbContext
{
    public RepairDeskDbContext(DbContextOptions<RepairDeskDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<RegisteredDevice> Devices => Set<RegisteredDevice>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<RmaCase> RmaCases => Set<RmaCase>();
    public DbSet<CaseEvent> CaseEvents => Set<CaseEvent>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<RegisteredDevice>(entity =>
        {
            entity.HasIndex(d => d.SerialNumber).IsUnique();
            entity.HasOne(d => d.Product)
                .WithMany(p => p.Devices)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasIndex(i => i.ProductId).IsUnique();
            entity.HasOne(i => i.Product)
                .WithOne(p => p.Inventory)
                .HasForeignKey<InventoryItem>(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(i => i.RowVersion).IsConcurrencyToken();
        });

        modelBuilder.Entity<RmaCase>(entity =>
        {
            entity.HasOne(c => c.Device)
                .WithMany(d => d.Cases)
                .HasForeignKey(c => c.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(c => c.RefundAmount).HasColumnType("decimal(18,2)");
            entity.Property(c => c.RowVersion).IsConcurrencyToken();
        });

        modelBuilder.Entity<CaseEvent>(entity =>
        {
            entity.HasOne(e => e.Case)
                .WithMany(c => c.Events)
                .HasForeignKey(e => e.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
        });
    }

    public override int SaveChanges()
    {
        RotateRowVersions();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        RotateRowVersions();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// SQL Server's rowversion column type auto-increments on every UPDATE at the engine level,
    /// but SQLite has no equivalent. Rotating the token here on every add/modify keeps optimistic
    /// concurrency behavior identical across both providers.
    /// </summary>
    private void RotateRowVersions()
    {
        foreach (var entry in ChangeTracker.Entries<IHasRowVersion>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
        }
    }
}
