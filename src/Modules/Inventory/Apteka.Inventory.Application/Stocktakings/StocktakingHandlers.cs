using Apteka.Catalog.Contracts;
using Apteka.Inventory.Application.Issuing;
using Apteka.Inventory.Application.Receiving;
using Apteka.Inventory.Contracts;
using Apteka.Inventory.Domain.StockMovements;
using Apteka.Inventory.Domain.Stocktakings;
using Apteka.SharedKernel;

namespace Apteka.Inventory.Application.Stocktakings;

/// <summary>„Започване на инвентаризация“: a protocol of the batches with stock in the chosen scope.</summary>
public sealed class StartStocktakingHandler(
    ICatalogLookup catalog,
    IStockQueries stock,
    IStocktakingRepository stocktakings,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public async Task<Result<Guid>> HandleAsync(StartStocktakingInput input, CancellationToken cancellationToken = default)
    {
        if (await stocktakings.AnyOpenAsync(cancellationToken))
        {
            return Result.Failure<Guid>(StocktakingErrors.AlreadyOpen);
        }

        IReadOnlyCollection<Guid>? productIds = null;
        var scope = new List<string>();
        if (input.GroupId is not null || input.ShelfId is not null)
        {
            productIds = (await catalog.ListProductsAsync(input.GroupId, withMinStockOnly: false, cancellationToken, input.ShelfId))
                .Select(p => p.ProductId)
                .ToList();
            if (input.GroupId is { } group)
            {
                scope.Add("Група: " + (await catalog.ListGroupsAsync(cancellationToken)).FirstOrDefault(g => g.Id == group)?.Name);
            }

            if (input.ShelfId is { } shelf)
            {
                scope.Add("Рафт: " + (await catalog.ListShelvesAsync(cancellationToken)).FirstOrDefault(s => s.Id == shelf)?.Name);
            }
        }

        var batches = await stock.GetAllBatchesAsync(productIds, null, cancellationToken);
        var names = (await catalog.GetProductsAsync([.. batches.Select(b => b.ProductId).Distinct()], cancellationToken))
            .ToDictionary(p => p.ProductId, p => p.Name);

        var started = Stocktaking.Start(
            Guid.CreateVersion7(),
            scope.Count == 0 ? "Всички стоки" : string.Join(", ", scope),
            batches.Select(b => new StocktakingLineSpec(
                b.ProductId,
                names.GetValueOrDefault(b.ProductId, "?"),
                b.Batch.BatchId,
                b.Batch.BatchNo,
                b.Batch.ExpiryDate,
                b.Batch.BaseQuantity,
                b.Batch.PurchasePricePerBaseUnit)).ToList(),
            currentUser.UserId,
            clock.GetUtcNow());
        if (started.IsFailure)
        {
            return Result.Failure<Guid>(started.Error);
        }

        stocktakings.Add(started.Value);
        var saved = await stocktakings.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? Result.Failure<Guid>(saved.Error) : Result.Success(started.Value.Id);
    }
}

/// <summary>„Намерено“ for one batch of an open protocol; saved at once, so nothing is lost if the page closes.</summary>
public sealed class CountStocktakingHandler(IStocktakingRepository stocktakings)
{
    public async Task<Result> HandleAsync(Guid id, Guid lineId, int? countedBaseQuantity, CancellationToken cancellationToken = default)
    {
        var stocktaking = await stocktakings.GetAsync(id, cancellationToken);
        if (stocktaking is null)
        {
            return Result.Failure(StocktakingErrors.NotFound);
        }

        var counted = stocktaking.Count(lineId, countedBaseQuantity);
        return counted.IsFailure ? counted : await stocktakings.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// „Приключване на инвентаризация“: locks the batches, turns every counted difference into a correction movement
/// and closes the protocol, in one transaction.
/// </summary>
public sealed class CloseStocktakingHandler(
    IStocktakingRepository stocktakings,
    IStockLocking locking,
    IInventoryRepository inventory,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public const string DocumentType = "Stocktaking";

    public Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(ct => CloseAsync(id, ct), cancellationToken);

    private async Task<Result> CloseAsync(Guid id, CancellationToken cancellationToken)
    {
        var stocktaking = await stocktakings.GetAsync(id, cancellationToken);
        if (stocktaking is null)
        {
            return Result.Failure(StocktakingErrors.NotFound);
        }

        var onHand = new Dictionary<Guid, int>();
        foreach (var productId in stocktaking.Lines.Select(l => l.ProductId).Distinct().Order())
        {
            foreach (var batch in await locking.LockRetailBatchesAsync(productId, cancellationToken))
            {
                onHand[batch.BatchId] = batch.Quantity;
            }
        }

        var now = clock.GetUtcNow();
        var closed = stocktaking.Close(onHand, currentUser.UserId, now);
        if (closed.IsFailure)
        {
            return closed;
        }

        foreach (var (batchId, delta) in closed.Value)
        {
            var movement = StockMovement.Correction(batchId, delta, DocumentType, stocktaking.Id, currentUser.UserId, now);
            if (movement.IsFailure)
            {
                return movement;
            }

            inventory.Add(movement.Value);
        }

        return await stocktakings.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ListStocktakingsHandler(IStocktakingRepository stocktakings)
{
    public const int DefaultLimit = 100;

    public Task<IReadOnlyList<StocktakingListItem>> HandleAsync(CancellationToken cancellationToken = default) =>
        stocktakings.ListAsync(DefaultLimit, cancellationToken);
}

public sealed class GetStocktakingHandler(IStocktakingRepository stocktakings)
{
    public async Task<StocktakingView?> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var s = await stocktakings.GetAsync(id, cancellationToken);
        return s is null
            ? null
            : new StocktakingView(
                s.Id,
                s.Number,
                s.Scope,
                s.Status,
                s.StartedAt,
                s.ClosedAt,
                s.Lines.OrderBy(l => l.Position)
                    .Select(l => new StocktakingLineView(l.Id, l.Position, l.ProductId, l.ProductName, l.BatchNo, l.ExpiryDate, l.ExpectedQuantity, l.CountedQuantity, l.Difference, l.DifferenceValue.Amount))
                    .ToList(),
                s.Lines.Aggregate(Money.Zero, (sum, l) => sum + l.DifferenceValue).Amount);
    }
}
