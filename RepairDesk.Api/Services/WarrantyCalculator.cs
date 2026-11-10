using RepairDesk.Api.Models;
using RepairDesk.Api.Models.Enums;

namespace RepairDesk.Api.Services;

public static class WarrantyCalculator
{
    public static DateTime CalculateExpiration(Product product, DateTime purchaseDate, WarrantyTier tier)
    {
        var months = tier == WarrantyTier.Extended
            ? product.ExtendedWarrantyMonths
            : product.StandardWarrantyMonths;

        return purchaseDate.AddMonths(months);
    }

    public static bool IsUnderWarranty(RegisteredDevice device, DateTime asOfUtc)
    {
        return asOfUtc <= device.WarrantyExpiresOn;
    }
}
