using Apteka.SharedKernel;

namespace Apteka.Inventory.Contracts;

/// <summary>
/// Puts received goods into stock: one batch and one stock movement per line.
/// Call it inside the caller's <see cref="IUnitOfWork"/> so that the document and the stock are saved together.
/// </summary>
public interface IStockReceiving
{
    Task<Result> ReceiveAsync(StockReceipt receipt, CancellationToken cancellationToken = default);
}

/// <param name="DocumentType">E.g. "Delivery".</param>
public sealed record StockReceipt(
    string DocumentType,
    Guid DocumentId,
    Guid? UserId,
    IReadOnlyList<StockReceiptLine> Lines);

/// <param name="DocumentLineId">Received only once: a second receipt of the same line is refused.</param>
/// <param name="BaseQuantity">Quantity in the product's base unit.</param>
/// <param name="PurchasePricePerBaseUnit">Without VAT.</param>
/// <param name="PricedUnitFactor">Factor of the unit the sale price is for.</param>
/// <param name="PricedUnitSalePrice">Sale price with VAT of that unit.</param>
public sealed record StockReceiptLine(
    Guid DocumentLineId,
    Guid ProductId,
    string BatchNo,
    DateOnly ExpiryDate,
    int BaseQuantity,
    decimal PurchasePricePerBaseUnit,
    int PricedUnitFactor,
    decimal PricedUnitSalePrice,
    StockKind StockKind = StockKind.Retail);

public enum StockKind
{
    Retail = 1,
    Nhif = 2,
}
