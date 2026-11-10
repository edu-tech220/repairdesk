namespace RepairDesk.Api.Models;

public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StandardWarrantyMonths { get; set; } = 12;
    public int ExtendedWarrantyMonths { get; set; } = 24;

    public ICollection<RegisteredDevice> Devices { get; set; } = new List<RegisteredDevice>();
    public InventoryItem? Inventory { get; set; }
}
