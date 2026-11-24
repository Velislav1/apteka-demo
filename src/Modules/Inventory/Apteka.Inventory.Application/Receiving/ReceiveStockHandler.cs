using Apteka.Inventory.Contracts;
using Apteka.Inventory.Domain.Batches;
using Apteka.Inventory.Domain.StockMovements;
using Apteka.SharedKernel;

namespace Apteka.Inventory.Application.Receiving;

/// <summary>Use case „Заприхождаване“: a new batch and a + movement for every receipt line.</summary>
public sealed class ReceiveStockHandler(IInventoryRepository inventory, TimeProvider clock) : IStockReceiving
{
    public async Task<Result> ReceiveAsync(StockReceipt receipt, CancellationToken cancellationToken = default)
    {
        if (receipt.Lines.Count == 0)
        {
            return Result.Failure(InventoryErrors.ReceiptEmpty);
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
