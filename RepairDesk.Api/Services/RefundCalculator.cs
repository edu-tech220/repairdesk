using RepairDesk.Api.Models;
using RepairDesk.Api.Models.Enums;

namespace RepairDesk.Api.Services;

/// <summary>
/// Prorates a refund by how much warranty coverage remains, with a tier-dependent
/// goodwill floor so an almost-expired Standard claim still gets something back
/// while an Extended-tier customer is guaranteed at least half the purchase price.
/// </summary>
public static class RefundCalculator
{
    private const decimal StandardFloor = 0.10m;
    private const decimal ExtendedFloor = 0.50m;

    public static decimal CalculateRefund(Product product, RegisteredDevice device, DateTime asOfUtc)
    {
        var totalCoverageDays = (device.WarrantyExpiresOn - device.PurchaseDate).TotalDays;
        if (totalCoverageDays <= 0)
        {
            return 0m;
        }

        var remainingDays = (device.WarrantyExpiresOn - asOfUtc).TotalDays;
        var remainingFraction = (decimal)Math.Clamp(remainingDays / totalCoverageDays, 0d, 1d);

        var floor = device.WarrantyTier == WarrantyTier.Extended ? ExtendedFloor : StandardFloor;
        var refundFactor = Math.Max(floor, remainingFraction);

        return Math.Round(product.Price * refundFactor, 2, MidpointRounding.AwayFromZero);
    }
}
