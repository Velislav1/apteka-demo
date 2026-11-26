using Apteka.Purchasing.Application.Accounting;
using Apteka.Purchasing.Domain.Deliveries;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Purchasing.Infrastructure.Accounting;

internal sealed class PurchaseLedgerQueries(PurchasingDbContext context) : IPurchaseLedgerQueries
{
    public async Task<IReadOnlyList<PurchaseDocument>> ListAsync(DateOnly firstDay, DateOnly lastDay, CancellationToken cancellationToken)
    {
        var invoices = await (
                from d in context.Deliveries.AsNoTracking()
                where d.Status == DeliveryStatus.Posted && d.InvoiceDate >= firstDay && d.InvoiceDate <= lastDay
                join s in context.Suppliers.AsNoTracking() on d.SupplierId equals s.Id
                select new
                {
                    d.InvoiceDate,
                    d.InvoiceNo,
                    SupplierName = s.Name,
                    s.Eik,
                    d.Number,
                    d.IsConsignment,
                    Lines = d.AllLines.Where(l => !l.IsRemoved).Select(l => new { l.ProductId, l.Quantity, l.UnitPurchasePrice, l.SupplierDiscountPercent }).ToList(),
                })
            .ToListAsync(cancellationToken);

        var corrections = await (
                from c in context.DeliveryCorrections.AsNoTracking()
                where c.NoteDate >= firstDay && c.NoteDate <= lastDay
                join d in context.Deliveries.AsNoTracking() on c.DeliveryId equals d.Id
                join s in context.Suppliers.AsNoTracking() on d.SupplierId equals s.Id
                select new
                {
                    c.NoteDate,
                    c.NoteNo,
                    c.Number,
                    SupplierName = s.Name,
                    s.Eik,
                    DeliveryNumber = d.Number,
                    d.IsConsignment,
                    Lines = c.Lines.Select(l => new { l.ProductId, l.QuantityDelta, l.UnitPurchasePrice }).ToList(),
                })
            .ToListAsync(cancellationToken);

        return
        [
            .. invoices.Select(i => new PurchaseDocument(
                i.InvoiceDate, false, i.InvoiceNo, i.SupplierName, i.Eik, i.Number, i.IsConsignment,
                [.. i.Lines.Select(l => new PurchaseDocumentLine(
                    l.ProductId, UnitPrice.Of(UnitPrice.Of(l.UnitPurchasePrice * (1 - (l.SupplierDiscountPercent / 100m))).Amount).Times(l.Quantity).Amount))])),
            .. corrections.Select(c => new PurchaseDocument(
                c.NoteDate, true, c.NoteNo ?? $"КСР {c.Number}", c.SupplierName, c.Eik, c.DeliveryNumber, c.IsConsignment,
                [.. c.Lines.Select(l => new PurchaseDocumentLine(
                    l.ProductId, Math.Sign(l.QuantityDelta) * UnitPrice.Of(l.UnitPurchasePrice).Times(Math.Abs(l.QuantityDelta)).Amount))])),
        ];
    }
}
