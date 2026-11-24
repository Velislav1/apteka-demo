using Apteka.Inventory.Domain.Batches;
using Apteka.SharedKernel;

namespace Apteka.Inventory.Domain.Allocation;

/// <summary>
/// Chooses the batches to issue from (rule 5): first expiry first out; expired batches are never issued;
/// batches with the same expiry are taken in the order they were received.
/// </summary>
public static class FefoAllocator
{
    public static Result<IReadOnlyList<BatchAllocation>> Allocate(
        IReadOnlyList<BatchAvailability> batches,
        int baseQuantity,
        DateOnly today)
    {
        if (baseQuantity <= 0)
        {
            return Result.Failure<IReadOnlyList<BatchAllocation>>(InventoryErrors.QuantityMustBePositive);
        }

        var sellable = batches
            .Where(b => b.Quantity > 0 && b.ExpiryDate >= today)
            .OrderBy(b => b.ExpiryDate)
            .ThenBy(b => b.ReceivedAt)
            .ThenBy(b => b.BatchId)
            .ToList();

        var available = sellable.Sum(b => b.Quantity);
        if (available < baseQuantity)
        {
            return Result.Failure<IReadOnlyList<BatchAllocation>>(InventoryErrors.InsufficientStock(available));
        }

        var allocations = new List<BatchAllocation>();
        var left = baseQuantity;
        foreach (var batch in sellable)
        {
            var take = Math.Min(left, batch.Quantity);
            allocations.Add(new BatchAllocation(batch.BatchId, take));
            left -= take;
            if (left == 0)
            {
                break;
            }
        }

        return Result.Success<IReadOnlyList<BatchAllocation>>(allocations);
    }
}

/// <param name="Quantity">Current quantity in base units (sum of movements).</param>
public sealed record BatchAvailability(Guid BatchId, DateOnly ExpiryDate, DateTimeOffset ReceivedAt, int Quantity);

public sealed record BatchAllocation(Guid BatchId, int BaseQuantity);
