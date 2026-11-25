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

    /// <summary>All batches with stock (optionally of some products / expiring by a date), for „Стокова наличност по партиди“.</summary>
    Task<IReadOnlyList<ProductBatchStock>> GetAllBatchesAsync(
        IReadOnlyCollection<Guid>? productIds,
        DateOnly? expiringOnOrBefore,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// „Картон на стока“: every movement of the product in the period (UTC, end exclusive), oldest first,
    /// with the quantity on hand before the period and after each movement.
    /// </summary>
    Task<ProductCard> GetProductCardAsync(Guid productId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sold per product in [fromUtc, toUtc), in base units: sales less reversals and returns (only products that sold).
    /// <paramref name="productIds"/> null = all products.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> GetSoldQuantitiesAsync(
        IReadOnlyCollection<Guid>? productIds,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Purchase price per base unit (without VAT) of the given batches, for profit in reports.</summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetPurchasePricesAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken = default);

    /// <summary>The prices of the product's most recently received batch (for „последни цени“), or null.</summary>
    Task<LastReceipt?> GetLastReceiptAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>One batch (with or without stock), or null.</summary>
    Task<BatchInfo?> GetBatchAsync(Guid batchId, CancellationToken cancellationToken = default);

    /// <summary>Batches of a product that still have stock, nearest expiry first.</summary>
    Task<IReadOnlyList<BatchStock>> GetBatchesAsync(
        Guid productId,
        DateOnly? expiringOnOrBefore,
        CancellationToken cancellationToken = default);
}

public sealed record ProductBatchStock(Guid ProductId, BatchStock Batch);

public sealed record ProductCard(int OpeningBaseQuantity, IReadOnlyList<CardMovement> Movements)
{
    public int ClosingBaseQuantity => Movements.Count == 0 ? OpeningBaseQuantity : Movements[^1].BalanceAfter;
}

/// <param name="Kind">Delivery, Sale, Reversal, WriteOff or Correction.</param>
public sealed record CardMovement(
    DateTimeOffset At,
    string Kind,
    string DocumentType,
    Guid DocumentId,
    string BatchNo,
    DateOnly ExpiryDate,
    int QuantityDelta,
    int BalanceAfter,
    Guid? UserId);

public sealed record LastReceipt(
    decimal PurchasePricePerBaseUnit,
    decimal SalePricePerBaseUnit,
    int PricedUnitFactor,
    decimal PricedUnitSalePrice,
    DateTimeOffset ReceivedAt);

/// <summary>One batch with where it came from and whether it is blocked, for the batch page.</summary>
public sealed record BatchInfo(
    Guid BatchId,
    Guid ProductId,
    string BatchNo,
    DateOnly ExpiryDate,
    int BaseQuantity,
    decimal PurchasePricePerBaseUnit,
    decimal SalePricePerBaseUnit,
    int PricedUnitFactor,
    decimal PricedUnitSalePrice,
    DateTimeOffset ReceivedAt,
    string SourceDocumentType,
    Guid SourceDocumentId,
    bool IsBlocked,
    IReadOnlyList<BatchBlockStep> BlockHistory);

public sealed record BatchBlockStep(bool IsBlocked, string? Reason, Guid? UserId, DateTimeOffset At);

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
    DateTimeOffset ReceivedAt,
    bool IsBlocked = false);
