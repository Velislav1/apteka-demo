using Apteka.Sales.Domain.Returns;
using Apteka.Sales.Application.Common;
using Apteka.Sales.Domain.Reversals;
using Apteka.Sales.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Sales.Infrastructure.Sales;

internal sealed class SaleRepository(SalesDbContext context) : ISaleRepository
{
    public Task<Sale?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Sales
            .Include(s => s.Lines).ThenInclude(l => l.Batches)
            .AsSplitQuery()
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Sale>> ListAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) =>
        await context.Sales.AsNoTracking()
            .Include(s => s.Lines).ThenInclude(l => l.Batches)
            .Where(s => s.CreatedAt >= fromUtc && s.CreatedAt < toUtc)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Sale>> ListForCustomerAsync(Guid customerId, int limit, CancellationToken cancellationToken) =>
        await context.Sales.AsNoTracking()
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.Number)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public void Add(Sale sale) => context.Sales.Add(sale);

    public void Add(Reversal reversal) => context.Reversals.Add(reversal);

    public void Add(SaleReturn saleReturn) => context.SaleReturns.Add(saleReturn);

    public async Task<IReadOnlyList<SaleReturn>> ListReturnsAsync(Guid saleId, CancellationToken cancellationToken) =>
        await context.SaleReturns.AsNoTracking()
            .Include(r => r.Lines).ThenInclude(l => l.Batches)
            .Where(r => r.SaleId == saleId)
            .OrderBy(r => r.Number)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReturnOfSale>> ListReturnsAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        var rows = await (
                from r in context.SaleReturns.AsNoTracking().Include(r => r.Lines).ThenInclude(l => l.Batches)
                join s in context.Sales on r.SaleId equals s.Id
                where r.CreatedAt >= fromUtc && r.CreatedAt < toUtc
                select new { Return = r, s.Number, s.PaymentMethod })
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
        return rows.Select(r => new ReturnOfSale(r.Return, r.Number, r.PaymentMethod)).ToList();
    }

    public async Task<IReadOnlyList<BatchSale>> ListSalesOfBatchAsync(Guid batchId, CancellationToken cancellationToken)
    {
        var taken = await (
                from b in context.SaleLineBatches.AsNoTracking()
                join l in context.SaleLines on b.SaleLineId equals l.Id
                join s in context.Sales on l.SaleId equals s.Id
                where b.BatchId == batchId
                select new { s.Id, s.Number, s.CreatedAt, s.Status, s.CustomerId, l.ProductName, b.BaseQuantity })
            .ToListAsync(cancellationToken);

        var returned = await (
                from rb in context.Set<SaleReturnLineBatch>().AsNoTracking()
                join rl in context.Set<SaleReturnLine>() on rb.ReturnLineId equals rl.Id
                join r in context.SaleReturns on rl.ReturnId equals r.Id
                where rb.BatchId == batchId
                select new { r.SaleId, rb.BaseQuantity })
            .ToListAsync(cancellationToken);

        return taken
            .GroupBy(t => new { t.Id, t.Number, t.CreatedAt, t.Status, t.CustomerId, t.ProductName })
            .Select(g => new BatchSale(g.Key.Id, g.Key.Number, g.Key.CreatedAt, g.Key.Status, g.Key.CustomerId, g.Key.ProductName,
                g.Sum(t => t.BaseQuantity), returned.Where(r => r.SaleId == g.Key.Id).Sum(r => r.BaseQuantity)))
            .OrderByDescending(s => s.CreatedAt)
            .ToList();
    }

    public Task<Sale?> FindByNumberAsync(long number, CancellationToken cancellationToken) =>
        context.Sales.AsNoTracking().SingleOrDefaultAsync(s => s.Number == number, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
