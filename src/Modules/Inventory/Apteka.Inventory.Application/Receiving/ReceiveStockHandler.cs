using Apteka.Inventory.Application.Issuing;
using Apteka.Inventory.Application.Stocktakings;
using Apteka.Inventory.Domain.Stocktakings;
using Apteka.Inventory.Contracts;
using Apteka.Inventory.Domain.Batches;
using Apteka.Inventory.Domain.StockMovements;
using Apteka.SharedKernel;

namespace Apteka.Inventory.Application.Receiving;

/// <summary>Use case „Заприхождаване“: a new batch and a + movement for every receipt line.</summary>
public sealed class ReceiveStockHandler(IInventoryRepository inventory, IStockLocking locking, IStocktakingRepository stocktakings, TimeProvider clock) : IStockReceiving
{
    public async Task<Result> CorrectAsync(StockCorrection correction, CancellationToken cancellationToken = default)
    {
        if (correction.Lines.Count == 0)
        {
            return Result.Failure(InventoryErrors.CorrectionEmpty);
        }

        var batches = (await inventory.GetBySourceLinesAsync([.. correction.Lines.Select(l => l.DocumentLineId)], cancellationToken))
            .ToDictionary(b => b.SourceLineId);
        if (await stocktakings.FindOpenForProductsAsync([.. batches.Values.Select(b => b.ProductId).Distinct()], cancellationToken) is { } counting)
        {
            return Result.Failure(StocktakingErrors.InStocktaking(counting));
        }

        // Lock the products in a fixed order (as the till), then check the − lines against what is left now.
        var left = new Dictionary<Guid, int>();
        foreach (var productId in batches.Values.Select(b => b.ProductId).Distinct().Order())
        {
            foreach (var locked in await locking.LockRetailBatchesAsync(productId, cancellationToken))
            {
                left[locked.BatchId] = locked.Quantity;
            }
        }

        var now = clock.GetUtcNow();
        foreach (var line in correction.Lines)
        {
            if (!batches.TryGetValue(line.DocumentLineId, out var batch) || !left.TryGetValue(batch.Id, out var quantity))
            {
                return Result.Failure(InventoryErrors.NotReceived(line.Position));
            }

            if (quantity + line.BaseQuantityDelta < 0)
            {
                return Result.Failure(InventoryErrors.CorrectionBelowZero(line.Position, batch.BatchNo, quantity));
            }

            var movement = StockMovement.Correction(batch.Id, line.BaseQuantityDelta, correction.DocumentType, correction.DocumentId, correction.UserId, now);
            if (movement.IsFailure)
            {
                return movement;
            }

            left[batch.Id] = quantity + line.BaseQuantityDelta;
            inventory.Add(movement.Value);
        }

        return await inventory.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result> ReceiveAsync(StockReceipt receipt, CancellationToken cancellationToken = default)
    {
        if (receipt.Lines.Count == 0)
        {
            return Result.Failure(InventoryErrors.ReceiptEmpty);
        }

        if (await stocktakings.FindOpenForProductsAsync([.. receipt.Lines.Select(l => l.ProductId).Distinct()], cancellationToken) is { } counting)
        {
            return Result.Failure(StocktakingErrors.InStocktaking(counting));
        }

        if (await inventory.AnyReceivedAsync([.. receipt.Lines.Select(l => l.DocumentLineId)], cancellationToken))
        {
            return Result.Failure(InventoryErrors.AlreadyReceived);
        }

        var now = clock.GetUtcNow();
        foreach (var line in receipt.Lines)
        {
            var batch = Batch.Create(
                new BatchSpec(
                    line.ProductId,
                    line.BatchNo,
                    line.ExpiryDate,
                    (StockType)line.StockKind,
                    line.PurchasePricePerBaseUnit,
                    line.PricedUnitFactor,
                    line.PricedUnitSalePrice,
                    receipt.DocumentType,
                    receipt.DocumentId,
                    line.DocumentLineId),
                now);
            if (batch.IsFailure)
            {
                return batch;
            }

            var movement = StockMovement.Receipt(batch.Value, line.BaseQuantity, receipt.DocumentType, receipt.DocumentId, receipt.UserId, now);
            if (movement.IsFailure)
            {
                return movement;
            }

            inventory.Add(batch.Value);
            inventory.Add(movement.Value);
        }

        return await inventory.SaveChangesAsync(cancellationToken);
    }
}
