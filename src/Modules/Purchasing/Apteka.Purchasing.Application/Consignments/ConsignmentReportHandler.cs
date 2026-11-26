using Apteka.Catalog.Contracts;
using Apteka.Inventory.Contracts;
using Apteka.Purchasing.Application.Payments;

namespace Apteka.Purchasing.Application.Consignments;

/// <summary>
/// „Отчет за консигнация“: per line of every consignment, what was received, returned, sold, written off and is
/// still in stock, and what is owed for it. The supplier is paid through „Задължения и плащания“.
/// </summary>
public sealed class ConsignmentReportHandler(IPaymentRepository payments, IStockQueries stock, ICatalogLookup catalog)
{
    /// <param name="onlyOpen">Leave out the lines with nothing left in stock and nothing owed (settled).</param>
    public async Task<ConsignmentReport> HandleAsync(Guid? supplierId, bool onlyOpen = false, CancellationToken cancellationToken = default)
    {
        var deliveries = (await payments.ListPostedDeliveriesAsync(supplierId, cancellationToken)).Where(d => d.IsConsignment).ToList();
        var lines = deliveries.SelectMany(d => (d.ConsignmentLines ?? []).Select(l => (Delivery: d, Line: l))).ToList();
        if (lines.Count == 0)
        {
            return new ConsignmentReport([], 0m, 0m);
        }

        var flows = await stock.GetSourceLineFlowsAsync([.. lines.Select(l => l.Line.DeliveryLineId)], cancellationToken);
        var products = (await catalog.GetProductsAsync([.. lines.Select(l => l.Line.ProductId).Distinct()], cancellationToken))
            .ToDictionary(p => p.ProductId);
        var paid = await payments.GetPaidAsync([.. deliveries.Select(d => d.Id)], cancellationToken);

        var rows = lines
            .Select(x =>
            {
                var (d, l) = x;
                var flow = flows.GetValueOrDefault(l.DeliveryLineId);
                var product = products.GetValueOrDefault(l.ProductId);
                return new ConsignmentRow(
                    d.Id,
                    d.Number,
                    d.SupplierName,
                    d.InvoiceNo,
                    d.InvoiceDate,
                    l.ProductId,
                    product?.Name ?? "?",
                    l.BatchNo,
                    l.ExpiryDate,
                    l.Quantity * l.UnitFactor,
                    -l.CorrectedQuantity * l.UnitFactor,
                    flow?.Sold ?? 0,
                    flow?.WrittenOff ?? 0,
                    flow?.OnHand ?? 0,
                    ConsignmentMath.ChargeableBaseQuantity(l, flow),
                    ConsignmentMath.Owed(l, flow, product?.VatRate ?? 0m));
            })
            .ToList();

        // What has been paid is per delivery; an open line still has stock, or its delivery is not paid in full.
        var owedPerDelivery = rows.GroupBy(r => r.DeliveryId).ToDictionary(g => g.Key, g => g.Sum(r => r.Owed));
        var unpaid = owedPerDelivery.Where(o => o.Value > paid.GetValueOrDefault(o.Key)).Select(o => o.Key).ToHashSet();
        var shown = rows
            .Where(r => !onlyOpen || r.OnHand > 0 || unpaid.Contains(r.DeliveryId))
            .OrderBy(r => r.SupplierName).ThenBy(r => r.Number).ThenBy(r => r.ProductName)
            .ToList();

        var ids = shown.Select(r => r.DeliveryId).ToHashSet();
        return new ConsignmentReport(
            shown,
            owedPerDelivery.Where(o => ids.Contains(o.Key)).Sum(o => o.Value),
            paid.Where(p => ids.Contains(p.Key)).Sum(p => p.Value));
    }
}

/// <param name="Owed">For the whole consignment so far (with VAT); <paramref name="Paid"/> is what was paid against it.</param>
public sealed record ConsignmentReport(IReadOnlyList<ConsignmentRow> Rows, decimal Owed, decimal Paid)
{
    public decimal Remaining => Owed - Paid;
}

/// <summary>Quantities in base units.</summary>
/// <param name="Returned">Given back to the supplier (with corrections; negative if more was added).</param>
/// <param name="Chargeable">What the pharmacy pays for: received − returned − on hand.</param>
/// <param name="Owed">For <paramref name="Chargeable"/>, with VAT.</param>
public sealed record ConsignmentRow(
    Guid DeliveryId,
    long Number,
    string SupplierName,
    string InvoiceNo,
    DateOnly InvoiceDate,
    Guid ProductId,
    string ProductName,
    string BatchNo,
    DateOnly ExpiryDate,
    int Received,
    int Returned,
    int Sold,
    int WrittenOff,
    int OnHand,
    int Chargeable,
    decimal Owed);
