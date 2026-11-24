using Apteka.Inventory.Application.Receiving;
using Apteka.Inventory.Application.Stocktakings;
using Apteka.Inventory.Domain.Stocktakings;
using Apteka.Inventory.Contracts;
using Apteka.Inventory.Domain.Allocation;
using Apteka.Inventory.Domain.Batches;
using Apteka.Inventory.Domain.StockMovements;
using Apteka.SharedKernel;

namespace Apteka.Inventory.Application.Issuing;

/// <summary>Use cases „Изписване“ (sale) and „Връщане“ (reversal) of stock.</summary>
public sealed class IssueStockHandler(IStockLocking locking, IInventoryRepository inventory, IStocktakingRepository stocktakings, TimeProvider clock) : IStockIssuing
{
    public async Task<Result<IReadOnlyList<IssuedLine>>> IssueAsync(StockIssue issue, CancellationToken cancellationToken = default)
    {
        if (issue.Lines.Count == 0)
        {
            return Result.Failure<IReadOnlyList<IssuedLine>>(InventoryErrors.IssueEmpty);
        }

        if (await stocktakings.FindOpenForProductsAsync([.. issue.Lines.Select(l => l.ProductId).Distinct()], cancellationToken) is { } counting)
        {
            return Result.Failure<IReadOnlyList<IssuedLine>>(StocktakingErrors.InStocktaking(counting));
        }

        // Lock products in a fixed order, so that two tills never wait for each other in a circle (deadlock).
        var available = new Dictionary<Guid, List<BatchAvailability>>();
        foreach (var productId in issue.Lines.Select(l => l.ProductId).Distinct().Order())
        {
            available[productId] = [.. await locking.LockRetailBatchesAsync(productId, cancellationToken)];
        }

        var now = clock.GetUtcNow();
        var allocated = new List<(StockIssueLine Line, IReadOnlyList<BatchAllocation> Batches)>();
        foreach (var line in issue.Lines)
        {
            var batches = available[line.ProductId];
            var allocation = FefoAllocator.Allocate(batches, line.BaseQuantity, issue.Today);
            if (allocation.IsFailure)
            {
                return Result.Failure<IReadOnlyList<IssuedLine>>(allocation.Error);
            }

            // Two lines of the same product share the locked quantities.
            foreach (var taken in allocation.Value)
            {
                var index = batches.FindIndex(b => b.BatchId == taken.BatchId);
                batches[index] = batches[index] with { Quantity = batches[index].Quantity - taken.BaseQuantity };

                var movement = StockMovement.Issue(taken.BatchId, taken.BaseQuantity, issue.DocumentType, issue.DocumentId, issue.UserId, now);
                if (movement.IsFailure)
                {
                    return Result.Failure<IReadOnlyList<IssuedLine>>(movement.Error);
                }

                inventory.Add(movement.Value);
            }

            allocated.Add((line, allocation.Value));
        }

        var saved = await inventory.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return Result.Failure<IReadOnlyList<IssuedLine>>(saved.Error);
        }

        var details = await locking.GetBatchesAsync([.. allocated.SelectMany(a => a.Batches).Select(b => b.BatchId).Distinct()], cancellationToken);
        return Result.Success<IReadOnlyList<IssuedLine>>(allocated
            .Select(a => new IssuedLine(
                a.Line.LineId,
                a.Line.ProductId,
                a.Batches.Select(b => ToIssued(details[b.BatchId], b.BaseQuantity)).ToList()))
            .ToList());
    }

    public async Task<Result> ReturnAsync(StockReturn stockReturn, CancellationToken cancellationToken = default)
    {
        if (stockReturn.Batches.Count == 0)
        {
            return Result.Failure(InventoryErrors.NothingToReturn);
        }

        if (await stocktakings.FindOpenForBatchesAsync([.. stockReturn.Batches.Select(b => b.BatchId).Distinct()], cancellationToken) is { } counting)
        {
            return Result.Failure(StocktakingErrors.InStocktaking(counting));
        }

        var batches = await locking.GetBatchesAsync([.. stockReturn.Batches.Select(b => b.BatchId).Distinct()], cancellationToken);
        var now = clock.GetUtcNow();
        foreach (var returned in stockReturn.Batches)
        {
            if (!batches.ContainsKey(returned.BatchId))
            {
                return Result.Failure(InventoryErrors.UnknownBatch);
            }

            var movement = StockMovement.Return(returned.BatchId, returned.BaseQuantity, stockReturn.DocumentType, stockReturn.DocumentId, stockReturn.UserId, now);
            if (movement.IsFailure)
            {
                return movement;
            }

            inventory.Add(movement.Value);
        }

        return await inventory.SaveChangesAsync(cancellationToken);
    }

    private static IssuedBatch ToIssued(Batch batch, int baseQuantity) => new(
        batch.Id,
        batch.BatchNo,
        batch.ExpiryDate,
        baseQuantity,
        batch.SalePrice,
        batch.PricedUnitFactor,
        batch.PricedUnitSalePrice);
}
