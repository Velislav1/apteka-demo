using Apteka.Inventory.Application.Issuing;
using Apteka.Inventory.Domain.Allocation;
using Apteka.Inventory.Domain.Batches;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Inventory.Infrastructure.Batches;

internal sealed class StockLocking(InventoryDbContext context) : IStockLocking
{
    public async Task<IReadOnlyList<BatchAvailability>> LockRetailBatchesAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Stock can only be issued inside a transaction (IUnitOfWork); otherwise the lock would not hold.");
        }

        // Rule 4: lock the product's batch rows until commit. A second till selling the same product waits here,
        // and then sees the quantities after the first sale.
        var retail = nameof(StockType.Retail);
        var lockedIds = await context.Database
            .SqlQuery<Guid>($"SELECT id AS \"Value\" FROM inventory.batches WHERE product_id = {productId} AND stock_type = {retail} ORDER BY id FOR UPDATE")
            .ToListAsync(cancellationToken);

        var batches = await context.Batches.AsNoTracking()
            .Where(b => lockedIds.Contains(b.Id))
            .Select(b => new
            {
                b.Id,
                b.ExpiryDate,
                b.CreatedAt,
                Quantity = context.StockMovements.Where(m => m.BatchId == b.Id).Sum(m => m.QuantityDelta),
            })
            .ToListAsync(cancellationToken);

        return batches.Select(b => new BatchAvailability(b.Id, b.ExpiryDate, b.CreatedAt, b.Quantity)).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, Batch>> GetBatchesAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken)
    {
        var ids = batchIds.ToArray();
        return await context.Batches.AsNoTracking().Where(b => ids.Contains(b.Id)).ToDictionaryAsync(b => b.Id, cancellationToken);
    }
}
