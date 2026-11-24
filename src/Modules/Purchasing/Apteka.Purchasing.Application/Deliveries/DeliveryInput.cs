namespace Apteka.Purchasing.Application.Deliveries;

/// <summary>What the delivery form sends when the draft is saved.</summary>
public sealed record DeliveryInput(
    Guid SupplierId,
    string InvoiceNo,
    DateOnly InvoiceDate,
    IReadOnlyList<DeliveryLineInput> Lines);

/// <param name="Id">Null for a new line.</param>
/// <param name="UnitPurchasePrice">Per <paramref name="ProductUnitId"/>, without VAT.</param>
/// <param name="SalePrice">Per <paramref name="ProductUnitId"/>, with VAT.</param>
public sealed record DeliveryLineInput(
    Guid? Id,
    Guid ProductId,
    Guid ProductUnitId,
    int Quantity,
    decimal UnitPurchasePrice,
    decimal? MarkupPercent,
    decimal SalePrice,
    string BatchNo,
    DateOnly ExpiryDate);
