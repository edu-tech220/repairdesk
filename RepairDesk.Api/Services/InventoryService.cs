using Microsoft.EntityFrameworkCore;
using RepairDesk.Api.Data;
using RepairDesk.Api.Exceptions;

namespace RepairDesk.Api.Services;

/// <summary>
/// Reserves and restocks replacement units. Quantity updates use optimistic
/// concurrency with a short retry loop instead of locking, since contention on a
/// single product row is expected to be rare and short-lived.
/// </summary>
public class InventoryService : IInventoryService
{
    private const int MaxRetries = 3;

    private readonly RepairDeskDbContext _db;

    public InventoryService(RepairDeskDbContext db)
    {
        _db = db;
    }

    public async Task<bool> TryReserveOneAsync(int productId, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var item = await _db.InventoryItems
                .AsTracking()
                .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);

            if (item is null)
            {
                throw new NotFoundException(nameof(Models.InventoryItem), productId);
            }

            if (item.QuantityOnHand <= 0)
            {
                return false;
            }

            item.QuantityOnHand -= 1;

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                _db.Entry(item).State = EntityState.Detached;
            }
        }

        throw new ConcurrencyConflictException(
            $"Could not reserve stock for product {productId} after {MaxRetries} attempts.");
    }

    public async Task RestockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Restock quantity must be positive.");
        }

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var item = await _db.InventoryItems
                .AsTracking()
                .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);

            if (item is null)
            {
                throw new NotFoundException(nameof(Models.InventoryItem), productId);
            }

            item.QuantityOnHand += quantity;

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateConcurrencyException)
            {
                _db.Entry(item).State = EntityState.Detached;
            }
        }

        throw new ConcurrencyConflictException(
            $"Could not restock product {productId} after {MaxRetries} attempts.");
    }
}
