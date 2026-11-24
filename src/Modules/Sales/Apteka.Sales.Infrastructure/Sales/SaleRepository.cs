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

    public void Add(Sale sale) => context.Sales.Add(sale);

    public void Add(Reversal reversal) => context.Reversals.Add(reversal);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
