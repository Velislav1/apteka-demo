using Apteka.Catalog.Contracts;
using Apteka.Purchasing.Application.Deliveries;
using Apteka.SharedKernel;

namespace Apteka.Purchasing.Application.Corrections;

/// <summary>„Корекционни складови разписки“ for a period (Sofia dates, both included).</summary>
public sealed class ListCorrectionsHandler(IDeliveryCorrectionRepository corrections)
{
    public Task<IReadOnlyList<CorrectionListItem>> HandleAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        corrections.ListAsync(BulgarianTime.StartOfDayUtc(from), BulgarianTime.StartOfDayUtc(to.AddDays(1)), cancellationToken);
}

/// <summary>What earlier corrections changed on each line of a delivery (in the line's unit), and their number.</summary>
public sealed class DeliveryCorrectionsHandler(IDeliveryCorrectionRepository corrections)
{
    public async Task<DeliveryCorrections> HandleAsync(Guid deliveryId, CancellationToken cancellationToken = default)
    {
        var list = await corrections.ListForDeliveryAsync(deliveryId, cancellationToken);
        return new DeliveryCorrections(
            list.SelectMany(c => c.Lines).GroupBy(l => l.DeliveryLineId).ToDictionary(g => g.Key, g => g.Sum(l => l.QuantityDelta)),
            list.Select(c => new DeliveryCorrectionItem(c.Id, c.Number, c.CreatedAt, c.NoteNo, c.PurchaseTotal.Amount)).ToList());
    }
}

public sealed record DeliveryCorrections(IReadOnlyDictionary<Guid, int> QuantityByLine, IReadOnlyList<DeliveryCorrectionItem> Items);

public sealed record DeliveryCorrectionItem(Guid Id, long Number, DateTimeOffset CreatedAt, string? NoteNo, decimal PurchaseTotal);

public sealed class GetCorrectionHandler(IDeliveryCorrectionRepository corrections, IDeliveryRepository deliveries, ICatalogLookup catalog)
{
    public async Task<CorrectionView?> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var correction = await corrections.GetAsync(id, cancellationToken);
        if (correction is null || await deliveries.GetAsync(correction.DeliveryId, cancellationToken) is not { } delivery)
        {
            return null;
        }

        var products = (await catalog.GetProductsAsync([.. correction.Lines.Select(l => l.ProductId).Distinct()], cancellationToken))
            .ToDictionary(p => p.ProductId);
        var deliveryLines = delivery.Lines.ToDictionary(l => l.Id);

        return new CorrectionView(
            correction.Id,
            correction.Number,
            correction.CreatedAt,
            delivery.Id,
            delivery.Number,
            delivery.InvoiceNo,
            correction.NoteNo,
            correction.NoteDate,
            correction.Reason,
            correction.Lines.OrderBy(l => l.Position).Select(l =>
            {
                var product = products.GetValueOrDefault(l.ProductId);
                var line = deliveryLines[l.DeliveryLineId];
                return new CorrectionLineView(
                    l.DeliveryLinePosition,
                    product?.Name ?? "?",
                    product?.Units.FirstOrDefault(u => u.UnitId == line.ProductUnitId)?.Name ?? string.Empty,
                    line.BatchNo,
                    l.QuantityDelta,
                    l.UnitPurchasePrice,
                    l.PurchaseTotal.Amount);
            }).ToList(),
            correction.PurchaseTotal.Amount);
    }
}
