using RepairDesk.Api.Models;
using RepairDesk.Api.Models.Enums;

namespace RepairDesk.Api.Data;

public static class DbSeeder
{
    public static async Task SeedCatalogAsync(RepairDeskDbContext db)
    {
        if (db.Products.Any())
        {
            return;
        }

        var laptop = new Product
        {
            Sku = "LAP-14-PRO",
            Name = "Aurora 14 Laptop",
            Category = "Laptops",
            Price = 1299.00m,
            StandardWarrantyMonths = 12,
            ExtendedWarrantyMonths = 24
        };
        var headphones = new Product
        {
            Sku = "AUD-NC-200",
            Name = "Aurora NC-200 Headphones",
            Category = "Audio",
            Price = 249.00m,
            StandardWarrantyMonths = 12,
            ExtendedWarrantyMonths = 24
        };
        var tablet = new Product
        {
            Sku = "TAB-10-LITE",
            Name = "Aurora 10 Lite Tablet",
            Category = "Tablets",
            Price = 429.00m,
            StandardWarrantyMonths = 12,
            ExtendedWarrantyMonths = 24
        };

        db.Products.AddRange(laptop, headphones, tablet);
        await db.SaveChangesAsync();

        db.InventoryItems.AddRange(
            new InventoryItem { ProductId = laptop.Id, QuantityOnHand = 5 },
            new InventoryItem { ProductId = headphones.Id, QuantityOnHand = 0 },
            new InventoryItem { ProductId = tablet.Id, QuantityOnHand = 2 }
        );

        db.Devices.Add(new RegisteredDevice
        {
            SerialNumber = "DEMO-LAP-0001",
            ProductId = laptop.Id,
            OwnerName = "Jordan Rivera",
            OwnerEmail = "jordan.rivera@example.com",
            PurchaseDate = DateTime.UtcNow.AddMonths(-3),
            WarrantyTier = WarrantyTier.Extended,
            WarrantyExpiresOn = DateTime.UtcNow.AddMonths(-3).AddMonths(laptop.ExtendedWarrantyMonths)
        });

        await db.SaveChangesAsync();
    }
}
