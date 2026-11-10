using RepairDesk.Api.Models.Enums;

namespace RepairDesk.Api.Models;

public class RegisteredDevice
{
    public int Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public WarrantyTier WarrantyTier { get; set; }
    public DateTime WarrantyExpiresOn { get; set; }

    public ICollection<RmaCase> Cases { get; set; } = new List<RmaCase>();
}
