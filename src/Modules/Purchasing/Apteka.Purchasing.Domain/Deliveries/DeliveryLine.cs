using Apteka.SharedKernel;

namespace Apteka.Purchasing.Domain.Deliveries;

/// <summary>
/// One line of a delivery: a quantity of one unit of a product, with batch, expiry and prices for that unit.
/// A line removed from a draft is only marked as removed (rule 6: nothing is deleted physically).
/// </summary>
public sealed class DeliveryLine : Entity<Guid>
{
    private DeliveryLine()
    {
        BatchNo = string.Empty;
    }

    internal DeliveryLine(Guid deliveryId, DeliveryLineSpec spec, int position)
        : base(Guid.CreateVersion7())
    {
        DeliveryId = deliveryId;
        BatchNo = string.Empty;
        Apply(spec, position);
    }

    public Guid DeliveryId { get; private set; }

    /// <summary>Order of the line on the document (1, 2, 3…).</summary>
    public int Position { get; private set; }

    public Guid ProductId { get; private set; }

    public Guid ProductUnitId { get; private set; }

    /// <summary>The unit's factor when the line was entered (factors never change in the catalog).</summary>
    public int UnitFactor { get; private set; }

    /// <summary>Quantity in <see cref="ProductUnitId"/>.</summary>
    public int Quantity { get; private set; }

    /// <summary>Цена ТЕ: the wholesaler's price of one unit, without VAT, before the discount (as on the invoice).</summary>
    public decimal UnitPurchasePrice { get; private set; }

    /// <summary>Отстъпка ТЕ %: the wholesaler's discount on this line.</summary>
    public decimal SupplierDiscountPercent { get; private set; }

    /// <summary>Покупна цена: what one unit really costs, without VAT (price − discount, „математически“ method).</summary>
    public decimal NetUnitPurchasePrice => UnitPrice.Of(UnitPurchasePrice * (1 - (SupplierDiscountPercent / 100m))).Amount;

    public decimal? MarkupPercent { get; private set; }

    /// <summary>Sale price of one unit, with VAT (as on the shelf).</summary>
    public decimal SalePrice { get; private set; }

    public string BatchNo { get; private set; }

    public DateOnly ExpiryDate { get; private set; }

    public bool IsRemoved { get; private set; }

    public int BaseQuantity => Quantity * UnitFactor;

    public Money PurchaseTotal => UnitPrice.Of(NetUnitPurchasePrice).Times(Quantity);

    internal void Apply(DeliveryLineSpec spec, int position)
    {
        Position = position;
        ProductId = spec.ProductId;
        ProductUnitId = spec.ProductUnitId;
        UnitFactor = spec.UnitFactor;
        Quantity = spec.Quantity;
        UnitPurchasePrice = UnitPrice.Of(spec.UnitPurchasePrice).Amount;
        SupplierDiscountPercent = spec.SupplierDiscountPercent;
        MarkupPercent = spec.MarkupPercent;
        SalePrice = UnitPrice.Of(spec.SalePrice).Amount;
        BatchNo = spec.BatchNo.Trim();
        ExpiryDate = spec.ExpiryDate;
    }

    internal void Remove() => IsRemoved = true;
}

/// <summary>A line as requested by the user. <paramref name="Id"/> is null for a new line.</summary>
public sealed record DeliveryLineSpec(
    Guid? Id,
    Guid ProductId,
    Guid ProductUnitId,
    int UnitFactor,
    int Quantity,
    decimal UnitPurchasePrice,
    decimal? MarkupPercent,
    decimal SalePrice,
    string BatchNo,
    DateOnly ExpiryDate,
    decimal SupplierDiscountPercent = 0m);
