using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RepairDesk.Api.Data;
using RepairDesk.Api.Models;
using RepairDesk.Api.Models.Enums;
using RepairDesk.Api.Services;

namespace RepairDesk.Api.Tests.Infrastructure;

public static class TestDataSeeder
{
    public const string AdminUsername = "admin";
    public const string AdminPassword = "Admin#12345";
    public const string AgentUsername = "agent";
    public const string AgentPassword = "Agent#12345";

    /// <summary>Idempotent: safe to call once per test method even though the fixture's database is shared across a whole test class.</summary>
    public static async Task SeedUsersAsync(ApiWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RepairDeskDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await AddUserIfMissingAsync(db, hasher, AdminUsername, AdminPassword, UserRole.Admin);
        await AddUserIfMissingAsync(db, hasher, AgentUsername, AgentPassword, UserRole.Agent);
    }

    private static async Task AddUserIfMissingAsync(RepairDeskDbContext db, IPasswordHasher hasher, string username, string password, UserRole role)
    {
        if (await db.Users.AnyAsync(u => u.Username == username))
        {
            return;
        }

        var (hash, salt) = hasher.HashPassword(password);
        db.Users.Add(new AppUser
        {
            Username = username,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = role,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    /// <summary>Registers a product with inventory and a device under the given warranty terms, returning the serial number.</summary>
    public static async Task<string> SeedDeviceAsync(
        ApiWebApplicationFactory factory,
        string serialNumber,
        int quantityOnHand = 1,
        WarrantyTier tier = WarrantyTier.Standard,
        int purchasedMonthsAgo = 1,
        decimal price = 500m)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RepairDeskDbContext>();

        var product = new Product
        {
            Sku = $"SKU-{Guid.NewGuid():N}"[..12],
            Name = "Test Device",
            Category = "Test",
            Price = price,
            StandardWarrantyMonths = 12,
            ExtendedWarrantyMonths = 24
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        db.InventoryItems.Add(new InventoryItem { ProductId = product.Id, QuantityOnHand = quantityOnHand });

        var purchaseDate = DateTime.UtcNow.AddMonths(-purchasedMonthsAgo);
        db.Devices.Add(new RegisteredDevice
        {
            SerialNumber = serialNumber,
            ProductId = product.Id,
            OwnerName = "Test Owner",
            OwnerEmail = "owner@example.com",
            PurchaseDate = purchaseDate,
            WarrantyTier = tier,
            WarrantyExpiresOn = WarrantyCalculator.CalculateExpiration(product, purchaseDate, tier)
        });

        await db.SaveChangesAsync();
        return serialNumber;
    }
}
