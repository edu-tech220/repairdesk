using RepairDesk.Api.Models;
using RepairDesk.Api.Models.Enums;
using RepairDesk.Api.Services;
using Xunit;

namespace RepairDesk.Api.Tests.UnitTests;

public class RefundCalculatorTests
{
    private static readonly DateTime PurchaseDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ExpiresOn = PurchaseDate.AddDays(100);

    private static Product MakeProduct() => new() { Price = 1000m };

    private static RegisteredDevice MakeDevice(WarrantyTier tier) => new()
    {
        PurchaseDate = PurchaseDate,
        WarrantyExpiresOn = ExpiresOn,
        WarrantyTier = tier
    };

    [Fact]
    public void CalculateRefund_IsFullPrice_RightAfterPurchase()
    {
        var refund = RefundCalculator.CalculateRefund(MakeProduct(), MakeDevice(WarrantyTier.Standard), PurchaseDate);

        Assert.Equal(1000m, refund);
    }

    [Fact]
    public void CalculateRefund_UsesRemainingFraction_AboveTheFloor()
    {
        // 10 of 100 days left = 10% remaining, equal to the Standard floor.
        var asOf = PurchaseDate.AddDays(90);

        var refund = RefundCalculator.CalculateRefund(MakeProduct(), MakeDevice(WarrantyTier.Standard), asOf);

        Assert.Equal(100m, refund);
    }

    [Fact]
    public void CalculateRefund_StandardTier_NeverGoesBelowTenPercentFloor()
    {
        // Only 1 of 100 days left: the raw fraction (1%) is below the 10% floor.
        var asOf = PurchaseDate.AddDays(99);

        var refund = RefundCalculator.CalculateRefund(MakeProduct(), MakeDevice(WarrantyTier.Standard), asOf);

        Assert.Equal(100m, refund);
    }

    [Fact]
    public void CalculateRefund_ExtendedTier_NeverGoesBelowFiftyPercentFloor()
    {
        var asOf = PurchaseDate.AddDays(99);

        var refund = RefundCalculator.CalculateRefund(MakeProduct(), MakeDevice(WarrantyTier.Extended), asOf);

        Assert.Equal(500m, refund);
    }

    [Fact]
    public void CalculateRefund_PastExpiration_ClampsToFloor()
    {
        var asOf = ExpiresOn.AddDays(10);

        var refund = RefundCalculator.CalculateRefund(MakeProduct(), MakeDevice(WarrantyTier.Standard), asOf);

        Assert.Equal(100m, refund);
    }
}
