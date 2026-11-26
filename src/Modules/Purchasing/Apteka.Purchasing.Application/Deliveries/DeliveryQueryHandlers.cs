using Apteka.Catalog.Contracts;
using Apteka.Purchasing.Domain.Deliveries;

namespace Apteka.Purchasing.Application.Deliveries;

/// <summary>„Доставки“ list, newest first.</summary>
public sealed class ListDeliveriesHandler(IDeliveryRepository deliveries)
{
    public const int DefaultLimit = 100;

    public Task<IReadOnlyList<DeliveryListItem>> HandleAsync(
        Guid? supplierId = null,
        DeliveryStatus? status = null,
        CancellationToken cancellationToken = default) =>
        deliveries.ListAsync(supplierId, status, DefaultLimit, cancellationToken);
}

/// <summary>One delivery for the form, with product and unit names from the catalog.</summary>
public sealed class GetDeliveryHandler(IDeliveryRepository deliveries, ICatalogLookup catalog)
{
    public async Task<DeliveryView?> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var delivery = await deliveries.GetAsync(id, cancellationToken);
        if (delivery is null)
        {
            return null;
        }

        var lines = delivery.Lines;
        var products = (await catalog.GetProductsAsync([.. lines.Select(l => l.ProductId).Distinct()], cancellationToken))
            .ToDictionary(p => p.ProductId);

        return new DeliveryView(
            delivery.Id,
            delivery.Number,
            delivery.PaymentTermDays,
            delivery.DueDate,
            delivery.SupplierId,
            delivery.InvoiceNo,
            delivery.InvoiceDate,
            delivery.ReceivedAt,
            delivery.Status,
            delivery.PostedAt,
            delivery.PurchaseTotal.Amount,
            lines.Select(l =>
            {
                var product = products.GetValueOrDefault(l.ProductId);
                return new DeliveryLineView(
                    l.Id,
                    l.ProductId,
                    product?.Name ?? "?",
                    product?.VatRate ?? 0m,
                    product?.Units ?? [],
                    l.ProductUnitId,
                    l.Quantity,
                    l.UnitPurchasePrice,
                    l.MarkupPercent,
                    l.SalePrice,
                    l.BatchNo,
                    l.ExpiryDate,
                    l.PurchaseTotal.Amount,
                    l.SupplierDiscountPercent,
                    l.NetUnitPurchasePrice);
            }).ToList(),
            delivery.IsConsignment);
    }
}

public sealed record DeliveryView(
    Guid Id,
    long Number,
    int PaymentTermDays,
    DateOnly DueDate,
    Guid SupplierId,
    string InvoiceNo,
    DateOnly InvoiceDate,
    DateTimeOffset ReceivedAt,
    DeliveryStatus Status,
    DateTimeOffset? PostedAt,
    decimal PurchaseTotal,
    IReadOnlyList<DeliveryLineView> Lines,
    bool IsConsignment = false);

public sealed record DeliveryLineView(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal VatRate,
    IReadOnlyList<CatalogUnit> Units,
    Guid ProductUnitId,
    int Quantity,
    decimal UnitPurchasePrice,
    decimal? MarkupPercent,
    decimal SalePrice,
    string BatchNo,
    DateOnly ExpiryDate,
    decimal PurchaseTotal,
    decimal SupplierDiscountPercent = 0m,
    decimal NetUnitPurchasePrice = 0m);
