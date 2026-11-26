using Apteka.SharedKernel;

namespace Apteka.Inventory.Domain.Batches;

/// <summary>
/// A batch of a product received with one delivery line: batch number, expiry date and prices.
/// Prices are kept per base unit (rule 2), plus the exact sale price of the unit it was priced in,
/// so that selling that unit later gives back exactly the price that was entered.
/// The quantity is never stored here: it is the sum of the batch's stock movements (rule 3).
/// </summary>
public sealed class Batch : Entity<Guid>
{
    public const int MaxBatchNoLength = 30;

    private Batch()
    {
        BatchNo = string.Empty;
        SourceDocumentType = string.Empty;
    }

    private Batch(Guid id)
        : base(id)
    {
        BatchNo = string.Empty;
        SourceDocumentType = string.Empty;
    }

    public Guid ProductId { get; private set; }

    public string BatchNo { get; private set; }

    public DateOnly ExpiryDate { get; private set; }

    public StockType StockType { get; private set; }

    /// <summary>Purchase price of one base unit, without VAT.</summary>
    public decimal PurchasePrice { get; private set; }

    /// <summary>Sale price of one base unit, with VAT.</summary>
    public decimal SalePrice { get; private set; }

    /// <summary>Factor of the unit the sale price was entered for (e.g. 30 for a box of 30).</summary>
    public int PricedUnitFactor { get; private set; }

    /// <summary>Sale price, with VAT, of the unit the price was entered for.</summary>
    public decimal PricedUnitSalePrice { get; private set; }

    public string SourceDocumentType { get; private set; }

    public Guid SourceDocumentId { get; private set; }

    /// <summary>The delivery line this batch came from; one batch per line, so a line can never be received twice.</summary>
    public Guid SourceLineId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsExpiredOn(DateOnly date) => ExpiryDate < date;

    /// <summary>„Промяна на цени“: a new sale price (with VAT) for the unit the batch is priced in.</summary>
    public Result ChangeSalePrice(decimal pricedUnitSalePrice)
    {
        if (pricedUnitSalePrice <= 0)
        {
            return Result.Failure(InventoryErrors.InvalidPrice);
        }

        var price = UnitPrice.Of(pricedUnitSalePrice);
        PricedUnitSalePrice = price.Amount;
        SalePrice = price.PerBaseUnit(PricedUnitFactor).Amount;
        return Result.Success();
    }

    public static Result<Batch> Create(BatchSpec spec, DateTimeOffset createdAt)
    {
        var batchNo = spec.BatchNo.Trim();
        if (batchNo.Length == 0)
        {
            return Result.Failure<Batch>(InventoryErrors.BatchNoRequired);
        }

        if (batchNo.Length > MaxBatchNoLength)
        {
            return Result.Failure<Batch>(InventoryErrors.BatchNoTooLong);
        }

        if (spec.PricedUnitFactor < 1)
        {
            return Result.Failure<Batch>(InventoryErrors.InvalidFactor);
        }

        if (spec.PurchasePricePerBaseUnit < 0 || spec.PricedUnitSalePrice <= 0)
        {
            return Result.Failure<Batch>(InventoryErrors.InvalidPrice);
        }

        if (!Enum.IsDefined(spec.StockType))
        {
            return Result.Failure<Batch>(InventoryErrors.InvalidStockType);
        }

        var pricedUnitSalePrice = UnitPrice.Of(spec.PricedUnitSalePrice);

        return Result.Success(new Batch(Guid.CreateVersion7())
        {
            ProductId = spec.ProductId,
            BatchNo = batchNo,
            ExpiryDate = spec.ExpiryDate,
            StockType = spec.StockType,
            PurchasePrice = UnitPrice.Of(spec.PurchasePricePerBaseUnit).Amount,
            SalePrice = pricedUnitSalePrice.PerBaseUnit(spec.PricedUnitFactor).Amount,
            PricedUnitFactor = spec.PricedUnitFactor,
            PricedUnitSalePrice = pricedUnitSalePrice.Amount,
            SourceDocumentType = spec.SourceDocumentType,
            SourceDocumentId = spec.SourceDocumentId,
            SourceLineId = spec.SourceLineId,
            CreatedAt = createdAt,
        });
    }
}

public sealed record BatchSpec(
    Guid ProductId,
    string BatchNo,
    DateOnly ExpiryDate,
    StockType StockType,
    decimal PurchasePricePerBaseUnit,
    int PricedUnitFactor,
    decimal PricedUnitSalePrice,
    string SourceDocumentType,
    Guid SourceDocumentId,
    Guid SourceLineId);
