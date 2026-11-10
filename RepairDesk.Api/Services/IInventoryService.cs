namespace RepairDesk.Api.Services;

public interface IInventoryService
{
    Task<bool> TryReserveOneAsync(int productId, CancellationToken cancellationToken = default);
    Task RestockAsync(int productId, int quantity, CancellationToken cancellationToken = default);
}
