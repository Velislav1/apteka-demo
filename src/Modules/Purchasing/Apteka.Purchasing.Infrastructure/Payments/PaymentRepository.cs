using Apteka.Purchasing.Application.Payments;
using Apteka.Purchasing.Domain.Deliveries;
using Apteka.Purchasing.Domain.Payments;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Purchasing.Infrastructure.Payments;

internal sealed class PaymentRepository(PurchasingDbContext context) : IPaymentRepository
{
    public void Add(SupplierPayment payment) => context.SupplierPayments.Add(payment);

    public async Task<IReadOnlyList<PayableDelivery>> ListPostedDeliveriesAsync(Guid? supplierId, CancellationToken cancellationToken)
    {
        var deliveries = context.Deliveries.AsNoTracking().Where(d => d.Status == DeliveryStatus.Posted);
        if (supplierId is { } id)
        {
            deliveries = deliveries.Where(d => d.SupplierId == id);
        }

        var rows = await (
                from d in deliveries
                join s in context.Suppliers on d.SupplierId equals s.Id
                select new
                {
                    d.Id,
                    d.Number,
                    d.SupplierId,
                    SupplierName = s.Name,
                    d.InvoiceNo,
                    d.InvoiceDate,
                    d.PaymentTermDays,
                    d.IsConsignment,
                    Lines = d.AllLines.Where(l => !l.IsRemoved)
                        .Select(l => new { l.Id, l.ProductId, l.Quantity, l.UnitFactor, l.UnitPurchasePrice, l.SupplierDiscountPercent, l.BatchNo, l.ExpiryDate })
                        .ToList(),
                })
            .ToListAsync(cancellationToken);

        var ids = rows.Select(r => r.Id).ToArray();
        var corrections = await context.DeliveryCorrections.AsNoTracking()
            .Where(c => ids.Contains(c.DeliveryId))
            .SelectMany(c => c.Lines.Select(l => new { c.DeliveryId, l.DeliveryLineId, l.ProductId, l.QuantityDelta, l.UnitPurchasePrice }))
            .ToListAsync(cancellationToken);

        return rows.Select(r =>
            {
                var corrected = corrections.Where(c => c.DeliveryId == r.Id).ToList();
                IReadOnlyList<PayableLine> lines = r.IsConsignment
                    ? []
                    : r.Lines.Select(l => new PayableLine(l.ProductId, UnitPrice.Of(NetPrice(l.UnitPurchasePrice, l.SupplierDiscountPercent)).Times(l.Quantity).Amount))
                        .Concat(corrected.Select(c => new PayableLine(c.ProductId,
                            c.QuantityDelta >= 0 ? UnitPrice.Of(c.UnitPurchasePrice).Times(c.QuantityDelta).Amount : -UnitPrice.Of(c.UnitPurchasePrice).Times(-c.QuantityDelta).Amount)))
                        .ToList();
                IReadOnlyList<ConsignmentLine> consigned = r.IsConsignment
                    ? r.Lines.Select(l => new ConsignmentLine(
                            l.Id,
                            l.ProductId,
                            l.BatchNo,
                            l.ExpiryDate,
                            l.UnitFactor,
                            l.Quantity,
                            corrected.Where(c => c.DeliveryLineId == l.Id).Sum(c => c.QuantityDelta),
                            NetPrice(l.UnitPurchasePrice, l.SupplierDiscountPercent)))
                        .ToList()
                    : [];
                return new PayableDelivery(r.Id, r.Number, r.SupplierId, r.SupplierName, r.InvoiceNo, r.InvoiceDate, r.PaymentTermDays, lines, r.IsConsignment, consigned);
            })
            .ToList();
    }

    private static decimal NetPrice(decimal unitPurchasePrice, decimal discountPercent) =>
        UnitPrice.Of(unitPurchasePrice * (1 - (discountPercent / 100m))).Amount;

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetPaidAsync(IReadOnlyCollection<Guid> deliveryIds, CancellationToken cancellationToken)
    {
        var ids = deliveryIds.ToArray();
        return await context.PaymentAllocations.AsNoTracking()
            .Where(a => ids.Contains(a.DeliveryId))
            .GroupBy(a => a.DeliveryId)
            .Select(g => new { g.Key, Paid = g.Sum(a => a.Amount) })
            .ToDictionaryAsync(g => g.Key, g => g.Paid, cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentListItem>> ListPaymentsAsync(Guid? supplierId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var payments = context.SupplierPayments.AsNoTracking().Where(p => p.PaidOn >= from && p.PaidOn <= to);
        if (supplierId is { } id)
        {
            payments = payments.Where(p => p.SupplierId == id);
        }

        return await (
                from p in payments
                join s in context.Suppliers on p.SupplierId equals s.Id
                orderby p.PaidOn descending, p.CreatedAt descending
                select new PaymentListItem(p.Id, p.PaidOn, s.Name, p.Amount, p.Channel, p.Note, p.Allocations.Count))
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
