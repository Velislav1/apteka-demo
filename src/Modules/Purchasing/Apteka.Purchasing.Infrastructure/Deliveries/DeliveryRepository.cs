using Apteka.Purchasing.Application.Deliveries;
using Apteka.Purchasing.Domain.Deliveries;
using Apteka.Purchasing.Infrastructure.Persistence.Configurations;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Apteka.Purchasing.Infrastructure.Deliveries;

internal sealed class DeliveryRepository(PurchasingDbContext context) : IDeliveryRepository
{
    public Task<Delivery?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Deliveries.Include(d => d.AllLines).SingleOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<bool> InvoiceExistsAsync(Guid supplierId, string invoiceNo, Guid exceptId, CancellationToken cancellationToken) =>
        context.Deliveries.AnyAsync(d => d.SupplierId == supplierId && d.InvoiceNo == invoiceNo && d.Id != exceptId, cancellationToken);

    public async Task<IReadOnlyList<DeliveryListItem>> ListAsync(
        Guid? supplierId,
        DeliveryStatus? status,
        int limit,
        CancellationToken cancellationToken)
    {
        var deliveries = context.Deliveries.AsNoTracking();
        if (supplierId is { } id)
        {
            deliveries = deliveries.Where(d => d.SupplierId == id);
        }

        if (status is { } s)
        {
            deliveries = deliveries.Where(d => d.Status == s);
        }

        var rows = await (
                from d in deliveries
                join supplier in context.Suppliers on d.SupplierId equals supplier.Id
                orderby d.ReceivedAt descending
                select new
                {
                    d.Id,
                    d.Number,
                    SupplierName = supplier.Name,
                    d.InvoiceNo,
                    d.InvoiceDate,
                    d.ReceivedAt,
                    d.Status,
                    Lines = d.AllLines.Where(l => !l.IsRemoved).Select(l => new { l.Quantity, l.UnitPurchasePrice, l.SupplierDiscountPercent }).ToList(),
                })
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new DeliveryListItem(
                r.Id,
                r.Number,
                r.SupplierName,
                r.InvoiceNo,
                r.InvoiceDate,
                r.ReceivedAt,
                r.Status,
                r.Lines.Count,
                r.Lines.Aggregate(Money.Zero, (sum, l) => sum + UnitPrice.Of(l.UnitPurchasePrice * (1 - (l.SupplierDiscountPercent / 100m))).Times(l.Quantity)).Amount))
            .ToList();
    }

    public void Add(Delivery delivery) => context.Deliveries.Add(delivery);

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: DeliveryConfiguration.UniqueInvoiceIndex,
        })
        {
            var invoiceNo = context.ChangeTracker.Entries<Delivery>().Select(e => e.Entity.InvoiceNo).FirstOrDefault() ?? string.Empty;
            context.ChangeTracker.Clear();
            return Result.Failure(DeliveryErrors.InvoiceTaken(invoiceNo));
        }
    }
}
