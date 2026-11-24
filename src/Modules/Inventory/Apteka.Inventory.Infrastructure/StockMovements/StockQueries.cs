using Apteka.Inventory.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Inventory.Infrastructure.StockMovements;

/// <summary>Stock = SUM(quantity_delta) of the movements of each batch (rule 3); nothing is stored ready-made.</summary>
internal sealed class StockQueries(InventoryDbContext context) : IStockQueries
{
    public async Task<IReadOnlyList<ProductStock>> GetProductStockAsync(
        IReadOnlyCollection<Guid>? productIds,
        DateOnly? expiringOnOrBefore,
        CancellationToken cancellationToken = default)
    {
        var batches = await BatchesWithStockAsync(productIds, expiringOnOrBefore, cancellationToken);

        return batches
            .GroupBy(b => b.ProductId)
            .Select(g => new ProductStock(g.Key, g.Sum(b => b.Quantity), g.Min(b => b.ExpiryDate), g.Count()))
            .ToList();
    }

    public async Task<IReadOnlyList<BatchStock>> GetBatchesAsync(
        Guid productId,
        DateOnly? expiringOnOrBefore,
        CancellationToken cancellationToken = default)
    {
        var stock = (await BatchesWithStockAsync([productId], expiringOnOrBefore, cancellationToken))
            .ToDictionary(s => s.BatchId, s => s.Quantity);
        var ids = stock.Keys.ToArray();

        var batches = await context.Batches.AsNoTracking()
            .Where(b => ids.Contains(b.Id))
            .OrderBy(b => b.ExpiryDate)
            .ThenBy(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        return batches
            .Select(b => new BatchStock(
                b.Id,
                b.BatchNo,
                b.ExpiryDate,
                stock[b.Id],
                b.PurchasePrice,
                b.SalePrice,
                b.PricedUnitFactor,
                b.PricedUnitSalePrice,
                b.CreatedAt))
            .ToList();
    }

    /// <summary>Batches with a positive quantity; the sum is computed by PostgreSQL.</summary>
    private async Task<List<BatchQuantity>> BatchesWithStockAsync(
        IReadOnlyCollection<Guid>? productIds,
        DateOnly? expiringOnOrBefore,
        CancellationToken cancellationToken)
    {
        var batches = context.Batches.AsNoTracking();
        if (productIds is not null)
        {
            var ids = productIds.ToArray();
            batches = batches.Where(b => ids.Contains(b.ProductId));
        }

        if (expiringOnOrBefore is { } date)
        {
            batches = batches.Where(b => b.ExpiryDate <= date);
        }

        var rows = await (
                from m in context.StockMovements.AsNoTracking()
                join b in batches on m.BatchId equals b.Id
                group m.QuantityDelta by new { b.Id, b.ProductId, b.ExpiryDate } into g
                select new { g.Key.Id, g.Key.ProductId, g.Key.ExpiryDate, Quantity = g.Sum() })
            .Where(r => r.Quantity > 0)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new BatchQuantity(r.Id, r.ProductId, r.ExpiryDate, r.Quantity)).ToList();
    }

    private sealed record BatchQuantity(Guid BatchId, Guid ProductId, DateOnly ExpiryDate, int Quantity);
}
