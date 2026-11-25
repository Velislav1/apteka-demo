using Apteka.Purchasing.Application.Orders;
using Apteka.Purchasing.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Purchasing.Infrastructure.Orders;

internal sealed class PurchaseOrderRepository(PurchasingDbContext context) : IPurchaseOrderRepository
{
    public void Add(PurchaseOrder order) => context.PurchaseOrders.Add(order);

    public Task<PurchaseOrder?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.PurchaseOrders.Include(o => o.AllLines).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PurchaseOrderListItem>> ListAsync(int limit, CancellationToken cancellationToken) =>
        await (
                from o in context.PurchaseOrders.AsNoTracking()
                join s in context.Suppliers on o.SupplierId equals s.Id
                orderby o.Number descending
                select new PurchaseOrderListItem(o.Id, o.Number, s.Name, o.Status, o.CreatedAt, o.SentAt, o.AllLines.Count(l => !l.IsRemoved)))
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
