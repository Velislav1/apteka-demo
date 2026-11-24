using Apteka.Inventory.Application.Stocktakings;
using Apteka.Inventory.Domain.Stocktakings;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Apteka.Inventory.Infrastructure.Stocktakings;

internal sealed class StocktakingRepository(InventoryDbContext context) : IStocktakingRepository
{
    internal const string OneOpenIndex = "ux_stocktakings_one_open";

    public void Add(Stocktaking stocktaking) => context.Stocktakings.Add(stocktaking);

    public Task<Stocktaking?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Stocktakings.Include(s => s.Lines).SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> AnyOpenAsync(CancellationToken cancellationToken) =>
        context.Stocktakings.AnyAsync(s => s.Status == StocktakingStatus.Open, cancellationToken);

    public async Task<long?> FindOpenForProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        var ids = productIds.ToArray();
        return await context.Stocktakings.AsNoTracking()
            .Where(s => s.Status == StocktakingStatus.Open && s.Lines.Any(l => ids.Contains(l.ProductId)))
            .Select(s => (long?)s.Number)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<long?> FindOpenForBatchesAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken)
    {
        var ids = batchIds.ToArray();
        var products = context.Batches.Where(b => ids.Contains(b.Id)).Select(b => b.ProductId);
        return await context.Stocktakings.AsNoTracking()
            .Where(s => s.Status == StocktakingStatus.Open && s.Lines.Any(l => products.Contains(l.ProductId)))
            .Select(s => (long?)s.Number)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StocktakingListItem>> ListAsync(int limit, CancellationToken cancellationToken)
    {
        var stocktakings = await context.Stocktakings.AsNoTracking()
            .Include(s => s.Lines)
            .OrderByDescending(s => s.Number)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return stocktakings.Select(s => new StocktakingListItem(
                s.Id,
                s.Number,
                s.Scope,
                s.Status,
                s.StartedAt,
                s.ClosedAt,
                s.Lines.Count,
                s.Lines.Count(l => l.CountedQuantity is not null),
                s.Lines.Aggregate(Money.Zero, (sum, l) => sum + l.DifferenceValue).Amount))
            .ToList();
    }

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: OneOpenIndex })
        {
            context.ChangeTracker.Clear();
            return Result.Failure(StocktakingErrors.AlreadyOpen);
        }
    }
}
