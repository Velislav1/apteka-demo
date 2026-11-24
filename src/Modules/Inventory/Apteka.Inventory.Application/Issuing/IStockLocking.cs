using Apteka.Inventory.Domain.Allocation;
using Apteka.Inventory.Domain.Batches;

namespace Apteka.Inventory.Application.Issuing;

/// <summary>Reads stock under a row lock. Implemented in Infrastructure.</summary>
public interface IStockLocking
{
    /// <summary>
    /// Locks the retail batches of the product (SELECT … FOR UPDATE) until the current transaction ends,
    /// then returns them with their current quantities. Throws if there is no transaction.
    /// </summary>
    Task<IReadOnlyList<BatchAvailability>> LockRetailBatchesAsync(Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, Batch>> GetBatchesAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken);
}
