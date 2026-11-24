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

    public async Task<IReadOnlyList<ProductBatchStock>> GetAllBatchesAsync(
        IReadOnlyCollection<Guid>? productIds,
        DateOnly? expiringOnOrBefore,
        CancellationToken cancellationToken = default)
    {
        var stock = (await BatchesWithStockAsync(productIds, expiringOnOrBefore, cancellationToken))
            .ToDictionary(s => s.BatchId);
        var ids = stock.Keys.ToArray();

        var batches = await context.Batches.AsNoTracking()
            .Where(b => ids.Contains(b.Id))
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync(cancellationToken);

        return batches
            .Select(b => new ProductBatchStock(b.ProductId, new BatchStock(
                b.Id, b.BatchNo, b.ExpiryDate, stock[b.Id].Quantity, b.PurchasePrice, b.SalePrice, b.PricedUnitFactor, b.PricedUnitSalePrice, b.CreatedAt)))
            .ToList();
    }

    public async Task<ProductCard> GetProductCardAsync(Guid productId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default)
    {
        var movements =
            from m in context.StockMovements.AsNoTracking()
            join b in context.Batches.AsNoTracking() on m.BatchId equals b.Id
            where b.ProductId == productId
            select new { m.CreatedAt, m.Kind, m.DocumentType, m.DocumentId, b.BatchNo, b.ExpiryDate, m.QuantityDelta, m.UserId, m.Id };

        var opening = await movements.Where(m => m.CreatedAt < fromUtc).SumAsync(m => m.QuantityDelta, cancellationToken);
        var rows = await movements
            .Where(m => m.CreatedAt >= fromUtc && m.CreatedAt < toUtc)
            .OrderBy(m => m.CreatedAt).ThenBy(m => m.Id)
            .ToListAsync(cancellationToken);

        var balance = opening;
        var card = new List<CardMovement>(rows.Count);
        foreach (var row in rows)
        {
            balance += row.QuantityDelta;
            card.Add(new CardMovement(row.CreatedAt, row.Kind.ToString(), row.DocumentType, row.DocumentId, row.BatchNo, row.ExpiryDate, row.QuantityDelta, balance, row.UserId));
        }

        return new ProductCard(opening, card);
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetPurchasePricesAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken = default)
    {
        var ids = batchIds.ToArray();
        return await context.Batches.AsNoTracking()
            .Where(b => ids.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.PurchasePrice, cancellationToken);
    }

    public async Task<LastReceipt?> GetLastReceiptAsync(Guid productId, CancellationToken cancellationToken = default) =>
        await context.Batches.AsNoTracking()
            .Where(b => b.ProductId == productId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new LastReceipt(b.PurchasePrice, b.SalePrice, b.PricedUnitFactor, b.PricedUnitSalePrice, b.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

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
