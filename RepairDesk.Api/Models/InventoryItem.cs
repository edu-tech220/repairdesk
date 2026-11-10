namespace RepairDesk.Api.Models;

public class InventoryItem : IHasRowVersion
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int QuantityOnHand { get; set; }

    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();
}
