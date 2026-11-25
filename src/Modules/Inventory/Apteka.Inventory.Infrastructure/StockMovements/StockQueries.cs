using Apteka.Inventory.Contracts;
using Apteka.Inventory.Domain.StockMovements;
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

        var blocked = await BlockedAsync(ids, cancellationToken);
        return batches
            .Select(b => new ProductBatchStock(b.ProductId, new BatchStock(
                b.Id, b.BatchNo, b.ExpiryDate, stock[b.Id].Quantity, b.PurchasePrice, b.SalePrice, b.PricedUnitFactor, b.PricedUnitSalePrice, b.CreatedAt,
                blocked.Contains(b.Id))))
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

    public async Task<IReadOnlyDictionary<Guid, int>> GetSoldQuantitiesAsync(
        IReadOnlyCollection<Guid>? productIds,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        var batches = context.Batches.AsNoTracking();
        if (productIds is not null)
        {
            var ids = productIds.ToArray();
            batches = batches.Where(b => ids.Contains(b.ProductId));
        }

        var rows = await (
                from m in context.StockMovements.AsNoTracking()
                join b in batches on m.BatchId equals b.Id
                where m.CreatedAt >= fromUtc && m.CreatedAt < toUtc
                    && (m.Kind == Domain.StockMovements.MovementKind.Sale || m.Kind == Domain.StockMovements.MovementKind.Reversal)
                group m.QuantityDelta by b.ProductId into g
                select new { ProductId = g.Key, Sold = -g.Sum() })
            .Where(r => r.Sold > 0)
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.ProductId, r => r.Sold);
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

        var blocked = await BlockedAsync(ids, cancellationToken);
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
                b.CreatedAt,
                blocked.Contains(b.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<TurnoverRow>> GetTurnoverAsync(
        IReadOnlyCollection<Guid>? productIds,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        var batches = context.Batches.AsNoTracking();
        if (productIds is not null)
        {
            var ids = productIds.ToArray();
            batches = batches.Where(b => ids.Contains(b.ProductId));
        }

        // One row per batch, kind and side of the period; summed per product in memory (small: a pharmacy's batches).
        var rows = await (
                from m in context.StockMovements.AsNoTracking()
                join b in batches on m.BatchId equals b.Id
                where m.CreatedAt < toUtc
                group m.QuantityDelta by new { b.ProductId, b.PurchasePrice, m.Kind, m.DocumentType, Before = m.CreatedAt < fromUtc } into g
                select new { g.Key.ProductId, g.Key.PurchasePrice, g.Key.Kind, g.Key.DocumentType, g.Key.Before, Quantity = g.Sum() })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.ProductId)
            .Select(g =>
            {
                var before = g.Where(r => r.Before).ToList();
                var during = g.Where(r => !r.Before).ToList();
                var received = during.Where(r => r.Kind == MovementKind.Delivery || (r.Kind == MovementKind.Correction && r.DocumentType != "Stocktaking")).Sum(r => r.Quantity);
                var sold = -during.Where(r => r.Kind is MovementKind.Sale or MovementKind.Reversal).Sum(r => r.Quantity);
                var writtenOff = -during.Where(r => r.Kind == MovementKind.WriteOff).Sum(r => r.Quantity);
                var counted = during.Where(r => r.Kind == MovementKind.Correction && r.DocumentType == "Stocktaking").Sum(r => r.Quantity);
                var opening = before.Sum(r => r.Quantity);
                return new TurnoverRow(
                    g.Key,
                    opening,
                    decimal.Round(before.Sum(r => r.Quantity * r.PurchasePrice), 2, MidpointRounding.AwayFromZero),
                    received,
                    sold,
                    writtenOff,
                    counted,
                    g.Sum(r => r.Quantity),
                    decimal.Round(g.Sum(r => r.Quantity * r.PurchasePrice), 2, MidpointRounding.AwayFromZero));
            })
            .Where(r => r.Opening != 0 || r.Closing != 0 || r.Received != 0 || r.Sold != 0 || r.WrittenOff != 0 || r.Counted != 0)
            .ToList();
    }

    public async Task<BatchInfo?> GetBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        var batch = await context.Batches.AsNoTracking().SingleOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        if (batch is null)
        {
            return null;
        }

        var quantity = await context.StockMovements.Where(m => m.BatchId == batchId).SumAsync(m => m.QuantityDelta, cancellationToken);
        var history = await context.BatchBlocks.AsNoTracking()
            .Where(b => b.BatchId == batchId)
            .OrderBy(b => b.At).ThenBy(b => b.Id)
            .Select(b => new BatchBlockStep(b.IsBlocked, b.Reason, b.UserId, b.At))
            .ToListAsync(cancellationToken);

        return new BatchInfo(
            batch.Id, batch.ProductId, batch.BatchNo, batch.ExpiryDate, quantity, batch.PurchasePrice, batch.SalePrice, batch.PricedUnitFactor,
            batch.PricedUnitSalePrice, batch.CreatedAt, batch.SourceDocumentType, batch.SourceDocumentId, history.LastOrDefault()?.IsBlocked == true, history);
    }

    /// <summary>The batches among <paramref name="batchIds"/> whose latest blocking step blocks them.</summary>
    internal async Task<HashSet<Guid>> BlockedAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken)
    {
        var ids = batchIds.ToArray();
        var steps = await context.BatchBlocks.AsNoTracking()
            .Where(b => ids.Contains(b.BatchId))
            .Select(b => new { b.BatchId, b.IsBlocked, b.At, b.Id })
            .ToListAsync(cancellationToken);
        return steps
            .GroupBy(s => s.BatchId)
            .Where(g => g.OrderBy(s => s.At).ThenBy(s => s.Id).Last().IsBlocked)
            .Select(g => g.Key)
            .ToHashSet();
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
