namespace Apteka.Inventory.Contracts;

/// <summary>Stock on hand, always computed from the stock movements.</summary>
public interface IStockQueries
{
    /// <summary>
    /// Stock per product (only products with stock). <paramref name="productIds"/> null = all products;
    /// <paramref name="expiringOnOrBefore"/> counts only batches that expire on or before that date.
    /// </summary>
    Task<IReadOnlyList<ProductStock>> GetProductStockAsync(
        IReadOnlyCollection<Guid>? productIds,
        DateOnly? expiringOnOrBefore,
        CancellationToken cancellationToken = default);

    /// <summary>Batches of a product that still have stock, nearest expiry first.</summary>
    Task<IReadOnlyList<BatchStock>> GetBatchesAsync(
        Guid productId,
        DateOnly? expiringOnOrBefore,
        CancellationToken cancellationToken = default);
}

public sealed record ProductStock(Guid ProductId, int BaseQuantity, DateOnly NearestExpiry, int BatchCount);

public sealed record BatchStock(
    Guid BatchId,
    string BatchNo,
    DateOnly ExpiryDate,
    int BaseQuantity,
    decimal PurchasePricePerBaseUnit,
    decimal SalePricePerBaseUnit,
    int PricedUnitFactor,
    decimal PricedUnitSalePrice,
    DateTimeOffset ReceivedAt);
