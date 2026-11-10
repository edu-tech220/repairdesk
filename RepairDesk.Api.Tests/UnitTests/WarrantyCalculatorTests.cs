using RepairDesk.Api.Models;
using RepairDesk.Api.Models.Enums;
using RepairDesk.Api.Services;
using Xunit;

namespace RepairDesk.Api.Tests.UnitTests;

public class WarrantyCalculatorTests
{
    private static Product MakeProduct() => new()
    {
        StandardWarrantyMonths = 12,
        ExtendedWarrantyMonths = 24
    };

    [Fact]
    public void CalculateExpiration_UsesStandardMonths_ForStandardTier()
    {
        var product = MakeProduct();
        var purchase = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var expiration = WarrantyCalculator.CalculateExpiration(product, purchase, WarrantyTier.Standard);

        Assert.Equal(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), expiration);
    }

    [Fact]
    public void CalculateExpiration_UsesExtendedMonths_ForExtendedTier()
    {
        var product = MakeProduct();
        var purchase = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var expiration = WarrantyCalculator.CalculateExpiration(product, purchase, WarrantyTier.Extended);

        Assert.Equal(new DateTime(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc), expiration);
    }

    [Fact]
    public void IsUnderWarranty_ReturnsTrue_BeforeExpiration()
    {
        var device = new RegisteredDevice { WarrantyExpiresOn = DateTime.UtcNow.AddDays(5) };

        Assert.True(WarrantyCalculator.IsUnderWarranty(device, DateTime.UtcNow));
    }

    [Fact]
    public void IsUnderWarranty_ReturnsFalse_AfterExpiration()
    {
        var device = new RegisteredDevice { WarrantyExpiresOn = DateTime.UtcNow.AddDays(-5) };

        Assert.False(WarrantyCalculator.IsUnderWarranty(device, DateTime.UtcNow));
    }
}
