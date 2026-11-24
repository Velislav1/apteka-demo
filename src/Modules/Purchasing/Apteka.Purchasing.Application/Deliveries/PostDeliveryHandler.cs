using Apteka.Catalog.Contracts;
using Apteka.Inventory.Contracts;
using Apteka.Purchasing.Domain.Deliveries;
using Apteka.SharedKernel;

namespace Apteka.Purchasing.Application.Deliveries;

/// <summary>
/// Use case „Публикувай“: locks the delivery and puts its lines into stock (batches + movements in Inventory),
/// all in one transaction. If anything fails, the delivery stays a draft and no stock appears.
/// </summary>
public sealed class PostDeliveryHandler(
    IDeliveryRepository deliveries,
    ICatalogLookup catalog,
    IStockReceiving stock,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public const string DocumentType = "Delivery";

    public Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(ct => PostAsync(id, ct), cancellationToken);

    private async Task<Result> PostAsync(Guid id, CancellationToken cancellationToken)
    {
        var delivery = await deliveries.GetAsync(id, cancellationToken);
        if (delivery is null)
        {
            return Result.Failure(DeliveryErrors.NotFound);
        }

        // Products may have been deactivated since the draft was saved.
        var inputs = delivery.Lines
            .Select(l => new DeliveryLineInput(l.Id, l.ProductId, l.ProductUnitId, l.Quantity, l.UnitPurchasePrice, l.MarkupPercent, l.SalePrice, l.BatchNo, l.ExpiryDate))
            .ToList();
        var checkedLines = await DeliveryLineRules.ToSpecsAsync(inputs, catalog, cancellationToken);
        if (checkedLines.IsFailure)
        {
            return checkedLines;
        }

        var now = clock.GetUtcNow();
        var posted = delivery.Post(BulgarianTime.Today(now), now);
        if (posted.IsFailure)
        {
            return posted;
        }

        var receipt = new StockReceipt(
            DocumentType,
            delivery.Id,
            currentUser.UserId,
            delivery.Lines.Select(l => new StockReceiptLine(
                l.Id,
                l.ProductId,
                l.BatchNo,
                l.ExpiryDate,
                l.BaseQuantity,
                UnitPrice.Of(l.UnitPurchasePrice).PerBaseUnit(l.UnitFactor).Amount,
                l.UnitFactor,
                l.SalePrice)).ToList());

        var received = await stock.ReceiveAsync(receipt, cancellationToken);
        if (received.IsFailure)
        {
            return received;
        }

        return await deliveries.SaveChangesAsync(cancellationToken);
    }
}
