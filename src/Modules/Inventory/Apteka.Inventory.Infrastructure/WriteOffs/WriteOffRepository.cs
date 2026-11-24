using Apteka.Inventory.Application.WriteOffs;
using Apteka.Inventory.Domain.WriteOffs;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Inventory.Infrastructure.WriteOffs;

internal sealed class WriteOffRepository(InventoryDbContext context) : IWriteOffRepository
{
    public void Add(WriteOff writeOff) => context.WriteOffs.Add(writeOff);

    public Task<WriteOff?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.WriteOffs.AsNoTracking().Include(w => w.Lines).SingleOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WriteOffListItem>> ListAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        var writeOffs = await context.WriteOffs.AsNoTracking()
            .Include(w => w.Lines)
            .Where(w => w.CreatedAt >= fromUtc && w.CreatedAt < toUtc)
            .OrderByDescending(w => w.Number)
            .ToListAsync(cancellationToken);

        return writeOffs
            .Select(w => new WriteOffListItem(w.Id, w.Number, w.CreatedAt, w.UserId, w.Reason, w.Lines.Count, w.PurchaseTotal.Amount))
            .ToList();
    }
}
