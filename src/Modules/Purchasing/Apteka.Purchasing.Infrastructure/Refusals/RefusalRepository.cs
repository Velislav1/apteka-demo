using Apteka.Purchasing.Application.Refusals;
using Apteka.Purchasing.Domain.Refusals;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Purchasing.Infrastructure.Refusals;

internal sealed class RefusalRepository(PurchasingDbContext context) : IRefusalRepository
{
    public void Add(Refusal refusal) => context.Refusals.Add(refusal);

    public async Task<IReadOnlyList<Refusal>> ListAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) =>
        await context.Refusals.AsNoTracking().Where(r => r.CreatedAt >= fromUtc && r.CreatedAt < toUtc).OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
