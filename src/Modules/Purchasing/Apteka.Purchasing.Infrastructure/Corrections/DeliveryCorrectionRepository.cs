using Apteka.Purchasing.Application.Corrections;
using Apteka.Purchasing.Domain.Corrections;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Purchasing.Infrastructure.Corrections;

internal sealed class DeliveryCorrectionRepository(PurchasingDbContext context) : IDeliveryCorrectionRepository
{
    public void Add(DeliveryCorrection correction) => context.DeliveryCorrections.Add(correction);

    public Task<DeliveryCorrection?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.DeliveryCorrections.AsNoTracking().Include(c => c.Lines).SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DeliveryCorrection>> ListForDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken) =>
        await context.DeliveryCorrections.AsNoTracking()
            .Include(c => c.Lines)
            .Where(c => c.DeliveryId == deliveryId)
            .OrderBy(c => c.Number)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CorrectionListItem>> ListAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        var rows = await (
                from c in context.DeliveryCorrections.AsNoTracking().Include(c => c.Lines)
                join d in context.Deliveries on c.DeliveryId equals d.Id
                join s in context.Suppliers on d.SupplierId equals s.Id
                where c.CreatedAt >= fromUtc && c.CreatedAt < toUtc
                orderby c.Number descending
                select new { Correction = c, d.Number, SupplierName = s.Name, d.InvoiceNo })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new CorrectionListItem(
                r.Correction.Id,
                r.Correction.Number,
                r.Correction.CreatedAt,
                r.Correction.DeliveryId,
                r.Number,
                r.SupplierName,
                r.InvoiceNo,
                r.Correction.NoteNo,
                r.Correction.NoteDate,
                r.Correction.PurchaseTotal.Amount))
            .ToList();
    }

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
