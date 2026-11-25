using Apteka.Inventory.Application.BatchBlocks;
using Apteka.Inventory.Domain.BatchBlocks;

namespace Apteka.Inventory.Infrastructure.BatchBlocks;

internal sealed class BatchBlockRepository(InventoryDbContext context) : IBatchBlockRepository
{
    public void Add(BatchBlock step) => context.BatchBlocks.Add(step);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
