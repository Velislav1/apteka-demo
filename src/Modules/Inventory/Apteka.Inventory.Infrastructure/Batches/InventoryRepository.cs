using Apteka.Inventory.Application.Receiving;
using Apteka.Inventory.Domain.Batches;
using Apteka.Inventory.Domain.StockMovements;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Apteka.Inventory.Infrastructure.Batches;

internal sealed class InventoryRepository(InventoryDbContext context) : IInventoryRepository
{
    public Task<bool> AnyReceivedAsync(IReadOnlyCollection<Guid> sourceLineIds, CancellationToken cancellationToken) =>
        context.Batches.AnyAsync(b => sourceLineIds.Contains(b.SourceLineId), cancellationToken);

    public void Add(Batch batch) => context.Batches.Add(batch);

    public void Add(StockMovement movement) => context.StockMovements.Add(movement);

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: UniqueSourceLineIndex,
        })
        {
            context.ChangeTracker.Clear();
            return Result.Failure(InventoryErrors.AlreadyReceived);
        }
    }

    internal const string UniqueSourceLineIndex = "ux_batches_source_line_id";
}
