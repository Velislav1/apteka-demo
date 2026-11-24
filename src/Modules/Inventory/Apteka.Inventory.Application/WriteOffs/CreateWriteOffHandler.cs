using Apteka.Catalog.Contracts;
using Apteka.Inventory.Application.Issuing;
using Apteka.Inventory.Application.Receiving;
using Apteka.Inventory.Domain.StockMovements;
using Apteka.Inventory.Domain.WriteOffs;
using Apteka.SharedKernel;

namespace Apteka.Inventory.Application.WriteOffs;

/// <summary>
/// Use case „Бракуване на стоки“: takes the chosen quantities out of the chosen batches (expired ones too)
/// and saves the document, in one transaction. The batches are locked first (rule 4), so a till can never sell
/// what is being written off, and the stock never goes below zero.
/// </summary>
public sealed class CreateWriteOffHandler(
    ICatalogLookup catalog,
    IStockLocking locking,
    IInventoryRepository inventory,
    IWriteOffRepository writeOffs,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public const string DocumentType = "WriteOff";

    public Task<Result<Guid>> HandleAsync(WriteOffInput input, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(ct => CreateAsync(input, ct), cancellationToken);

    private async Task<Result<Guid>> CreateAsync(WriteOffInput input, CancellationToken cancellationToken)
    {
        if (input.Lines.Count == 0)
        {
            return Result.Failure<Guid>(WriteOffErrors.NoLines);
        }

        var products = (await catalog.GetProductsAsync([.. input.Lines.Select(l => l.ProductId).Distinct()], cancellationToken))
            .ToDictionary(p => p.ProductId);

        // Lock in a fixed order (by product id), as the till does, so the two never wait for each other in a circle.
        var available = new Dictionary<Guid, (Guid ProductId, int Quantity)>();
        foreach (var productId in products.Keys.Order())
        {
            foreach (var batch in await locking.LockRetailBatchesAsync(productId, cancellationToken))
            {
                available[batch.BatchId] = (productId, batch.Quantity);
            }
        }

        var details = await locking.GetBatchesAsync([.. input.Lines.Select(l => l.BatchId).Distinct()], cancellationToken);
        var specs = new List<WriteOffLineSpec>();
        for (var i = 0; i < input.Lines.Count; i++)
        {
            var line = input.Lines[i];
            var number = i + 1;
            if (!products.TryGetValue(line.ProductId, out var product) || product.Units.FirstOrDefault(u => u.UnitId == line.UnitId) is not { } unit)
            {
                return Result.Failure<Guid>(WriteOffErrors.ProductNotFound(number));
            }

            if (!available.TryGetValue(line.BatchId, out var stock) || stock.ProductId != line.ProductId || !details.TryGetValue(line.BatchId, out var batch))
            {
                return Result.Failure<Guid>(WriteOffErrors.BatchNotOfProduct(number));
            }

            if (line.Quantity <= 0)
            {
                return Result.Failure<Guid>(WriteOffErrors.QuantityMustBePositive(number));
            }

            var baseQuantity = line.Quantity * unit.Factor;
            if (baseQuantity > stock.Quantity)
            {
                return Result.Failure<Guid>(WriteOffErrors.NotEnoughInBatch(number, batch.BatchNo, stock.Quantity));
            }

            available[line.BatchId] = stock with { Quantity = stock.Quantity - baseQuantity };
            specs.Add(new WriteOffLineSpec(product.ProductId, product.Name, unit.Name, unit.Factor, line.Quantity, batch.Id, batch.BatchNo, batch.ExpiryDate, batch.PurchasePrice));
        }

        var now = clock.GetUtcNow();
        var created = WriteOff.Create(Guid.CreateVersion7(), input.Reason, specs, currentUser.UserId, now);
        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        var writeOff = created.Value;
        foreach (var line in writeOff.Lines)
        {
            var movement = StockMovement.WriteOff(line.BatchId, line.BaseQuantity, DocumentType, writeOff.Id, currentUser.UserId, now);
            if (movement.IsFailure)
            {
                return Result.Failure<Guid>(movement.Error);
            }

            inventory.Add(movement.Value);
        }

        writeOffs.Add(writeOff);
        var saved = await inventory.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? Result.Failure<Guid>(saved.Error) : Result.Success(writeOff.Id);
    }
}
