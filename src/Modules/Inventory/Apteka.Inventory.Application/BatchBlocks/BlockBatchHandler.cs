using Apteka.Inventory.Contracts;
using Apteka.Inventory.Domain.BatchBlocks;
using Apteka.SharedKernel;

namespace Apteka.Inventory.Application.BatchBlocks;

public interface IBatchBlockRepository
{
    void Add(BatchBlock step);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>„Блокиране на партида“ (recall) and releasing it again; every step is kept with who, when and why.</summary>
public sealed class BlockBatchHandler(IStockQueries stock, IBatchBlockRepository blocks, IUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider clock)
{
    public Task<Result> HandleAsync(Guid batchId, bool block, string? reason, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            if (await stock.GetBatchAsync(batchId, ct) is not { } batch)
            {
                return Result.Failure(BatchBlockErrors.BatchNotFound);
            }

            var step = BatchBlock.Create(batchId, block, batch.IsBlocked, reason, currentUser.UserId, clock.GetUtcNow());
            if (step.IsFailure)
            {
                return step;
            }

            blocks.Add(step.Value);
            await blocks.SaveChangesAsync(ct);
            return Result.Success();
        }, cancellationToken);
}
