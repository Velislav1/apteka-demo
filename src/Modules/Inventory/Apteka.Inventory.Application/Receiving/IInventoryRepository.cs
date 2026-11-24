using Apteka.Inventory.Domain.Batches;
using Apteka.Inventory.Domain.StockMovements;
using Apteka.SharedKernel;

namespace Apteka.Inventory.Application.Receiving;

/// <summary>Writes batches and movements. Implemented in Infrastructure.</summary>
public interface IInventoryRepository
{
    Task<bool> AnyReceivedAsync(IReadOnlyCollection<Guid> sourceLineIds, CancellationToken cancellationToken);

    void Add(Batch batch);

    void Add(StockMovement movement);

    /// <summary>Saves; a line received by a concurrent save is reported as <c>AlreadyReceived</c>, not an exception.</summary>
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}
