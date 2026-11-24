using Apteka.Catalog.Contracts;
using Apteka.Purchasing.Application.Suppliers;
using Apteka.Purchasing.Domain.Deliveries;
using Apteka.Purchasing.Domain.Suppliers;
using Apteka.SharedKernel;

namespace Apteka.Purchasing.Application.Deliveries;

/// <summary>Use case „Запиши“: creates a draft delivery or saves changes to one.</summary>
/// <remarks>Runs in <see cref="IUnitOfWork"/>: if a check fails after the draft was changed, the change is rolled back and forgotten.</remarks>
public sealed class SaveDeliveryDraftHandler(
    IDeliveryRepository deliveries,
    ISupplierRepository suppliers,
    ICatalogLookup catalog,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public Task<Result<Guid>> HandleAsync(Guid? id, DeliveryInput input, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(ct => SaveAsync(id, input, ct), cancellationToken);

    private async Task<Result<Guid>> SaveAsync(Guid? id, DeliveryInput input, CancellationToken cancellationToken)
    {
        Delivery delivery;
        Guid? previousSupplier = null;

        if (id is { } existingId)
        {
            var found = await deliveries.GetAsync(existingId, cancellationToken);
            if (found is null)
            {
                return Result.Failure<Guid>(DeliveryErrors.NotFound);
            }

            delivery = found;
            previousSupplier = delivery.SupplierId;
            var header = delivery.UpdateHeader(input.SupplierId, input.InvoiceNo, input.InvoiceDate);
            if (header.IsFailure)
            {
                return Result.Failure<Guid>(header.Error);
            }
        }
        else
        {
            var created = Delivery.Create(input.SupplierId, input.InvoiceNo, input.InvoiceDate, clock.GetUtcNow(), currentUser.UserId);
            if (created.IsFailure)
            {
                return Result.Failure<Guid>(created.Error);
            }

            delivery = created.Value;
        }

        if (input.SupplierId != previousSupplier)
        {
            var supplier = await suppliers.GetAsync(input.SupplierId, cancellationToken);
            if (supplier is not { IsActive: true })
            {
                return Result.Failure<Guid>(SupplierErrors.NotAvailable);
            }
        }

        if (await deliveries.InvoiceExistsAsync(delivery.SupplierId, delivery.InvoiceNo, delivery.Id, cancellationToken))
        {
            return Result.Failure<Guid>(DeliveryErrors.InvoiceTaken(delivery.InvoiceNo));
        }

        var lines = await DeliveryLineRules.ToSpecsAsync(input.Lines, catalog, cancellationToken);
        if (lines.IsFailure)
        {
            return Result.Failure<Guid>(lines.Error);
        }

        var set = delivery.SetLines(lines.Value);
        if (set.IsFailure)
        {
            return Result.Failure<Guid>(set.Error);
        }

        if (id is null)
        {
            deliveries.Add(delivery);
        }

        var saved = await deliveries.SaveChangesAsync(cancellationToken);
        return saved.IsSuccess ? Result.Success(delivery.Id) : Result.Failure<Guid>(saved.Error);
    }
}

internal static class DeliveryLineRules
{
    /// <summary>Checks every line's product and unit against the catalog and takes the unit's factor from it.</summary>
    public static async Task<Result<List<DeliveryLineSpec>>> ToSpecsAsync(
        IReadOnlyList<DeliveryLineInput> lines,
        ICatalogLookup catalog,
        CancellationToken cancellationToken)
    {
        var products = (await catalog.GetProductsAsync([.. lines.Select(l => l.ProductId).Distinct()], cancellationToken))
            .ToDictionary(p => p.ProductId);
        var specs = new List<DeliveryLineSpec>(lines.Count);

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (!products.TryGetValue(line.ProductId, out var product) || !product.IsActive)
            {
                return Result.Failure<List<DeliveryLineSpec>>(DeliveryErrors.ProductNotAvailable(i + 1));
            }

            var unit = product.Units.FirstOrDefault(u => u.UnitId == line.ProductUnitId);
            if (unit is null)
            {
                return Result.Failure<List<DeliveryLineSpec>>(DeliveryErrors.InvalidUnit(i + 1));
            }

            specs.Add(new DeliveryLineSpec(
                line.Id,
                line.ProductId,
                line.ProductUnitId,
                unit.Factor,
                line.Quantity,
                line.UnitPurchasePrice,
                line.MarkupPercent,
                line.SalePrice,
                line.BatchNo,
                line.ExpiryDate));
        }

        return Result.Success(specs);
    }
}
