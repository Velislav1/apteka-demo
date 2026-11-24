using Apteka.SharedKernel;

namespace Apteka.Sales.Domain.Sales;

/// <summary>
/// What the customer bought: a quantity of one unit of a product at one price.
/// The batches it was taken from are in <see cref="Batches"/> (FEFO may split a line over several batches).
/// Product and unit names are copied, so an old receipt never changes when the catalog does.
/// </summary>
public sealed class SaleLine : Entity<Guid>
{
    private readonly List<SaleLineBatch> _batches = [];

    private SaleLine()
    {
        ProductName = string.Empty;
        UnitName = string.Empty;
    }

    internal SaleLine(Guid saleId, int position, SaleLineSpec spec, Pricing.LineAmounts amounts)
        : base(spec.LineId)
    {
        SaleId = saleId;
        Position = position;
        ProductId = spec.ProductId;
        ProductName = spec.ProductName;
        ProductUnitId = spec.ProductUnitId;
        UnitName = spec.UnitName;
        UnitFactor = spec.UnitFactor;
        Quantity = spec.Quantity;
        UnitPrice = spec.UnitPrice;
        DiscountPercent = spec.DiscountPercent;
        VatRate = spec.VatRate;
        DiscountAmount = amounts.Discount;
        LineTotal = amounts.Total;
        VatAmount = amounts.Vat;
        _batches.AddRange(spec.Batches.Select((b, i) => new SaleLineBatch(Id, i + 1, b.BatchId, b.BaseQuantity)));
    }

    public Guid SaleId { get; private set; }

    public int Position { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; }

    public Guid ProductUnitId { get; private set; }

    public string UnitName { get; private set; }

    public int UnitFactor { get; private set; }

    /// <summary>In <see cref="ProductUnitId"/>.</summary>
    public int Quantity { get; private set; }

    /// <summary>Price of one unit, VAT included.</summary>
    public decimal UnitPrice { get; private set; }

    public decimal DiscountPercent { get; private set; }

    public decimal DiscountAmount { get; private set; }

    /// <summary>VAT rate when sold (the product's rate may change later).</summary>
    public decimal VatRate { get; private set; }

    public decimal LineTotal { get; private set; }

    public decimal VatAmount { get; private set; }

    public IReadOnlyList<SaleLineBatch> Batches => _batches;
}

/// <summary>A part of a sale line taken from one batch, in base units. Used to put it back on reversal.</summary>
public sealed class SaleLineBatch : Entity<Guid>
{
    private SaleLineBatch()
    {
    }

    internal SaleLineBatch(Guid saleLineId, int position, Guid batchId, int baseQuantity)
        : base(Guid.CreateVersion7())
    {
        SaleLineId = saleLineId;
        Position = position;
        BatchId = batchId;
        BaseQuantity = baseQuantity;
    }

    public Guid SaleLineId { get; private set; }

    /// <summary>Order in which FEFO took the batches (1 = nearest expiry); 0 for sales made before it was kept.</summary>
    public int Position { get; private set; }

    public Guid BatchId { get; private set; }

    public int BaseQuantity { get; private set; }
}

/// <param name="LineId">Chosen before the stock is issued, so Inventory and Sales use the same id.</param>
public sealed record SaleLineSpec(
    Guid LineId,
    Guid ProductId,
    string ProductName,
    Guid ProductUnitId,
    string UnitName,
    int UnitFactor,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal VatRate,
    IReadOnlyList<SaleLineBatchSpec> Batches);

public sealed record SaleLineBatchSpec(Guid BatchId, int BaseQuantity);
