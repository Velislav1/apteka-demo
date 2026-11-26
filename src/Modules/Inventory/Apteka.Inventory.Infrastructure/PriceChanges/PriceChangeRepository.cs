using Apteka.Inventory.Application.PriceChanges;
using Apteka.Inventory.Domain.Batches;
using Apteka.Inventory.Domain.PriceChanges;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Inventory.Infrastructure.PriceChanges;

internal sealed class PriceChangeRepository(InventoryDbContext context) : IPriceChangeRepository
{
    public void Add(PriceChange change) => context.PriceChanges.Add(change);

    public async Task<IReadOnlyList<Batch>> GetBatchesAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken)
    {
        var ids = batchIds.ToArray();
        return await context.Batches.Where(b => ids.Contains(b.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PriceChange>> ListAsync(int limit, CancellationToken cancellationToken) =>
        await context.PriceChanges.AsNoTracking().Include(c => c.Lines).OrderByDescending(c => c.Number).Take(limit).ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
