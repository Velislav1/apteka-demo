using Apteka.Inventory.Contracts;
using Apteka.Purchasing.Application.Deliveries;
using Apteka.Purchasing.Domain.Corrections;
using Apteka.SharedKernel;

namespace Apteka.Purchasing.Application.Corrections;

/// <summary>
/// Use case „Корекционна складова разписка“: saves the correction and moves the stock of the lines' batches
/// (+/−) in one transaction. A − that the batch no longer has (already sold) is refused and nothing changes.
/// </summary>
public sealed class CreateCorrectionHandler(
    IDeliveryRepository deliveries,
    IDeliveryCorrectionRepository corrections,
    IStockReceiving stock,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public const string DocumentType = "DeliveryCorrection";

    public Task<Result<Guid>> HandleAsync(CorrectionInput input, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(ct => CreateAsync(input, ct), cancellationToken);

    private async Task<Result<Guid>> CreateAsync(CorrectionInput input, CancellationToken cancellationToken)
    {
        var delivery = await deliveries.GetAsync(input.DeliveryId, cancellationToken);
        if (delivery is null)
        {
            return Result.Failure<Guid>(CorrectionErrors.DeliveryNotFound);
        }

        var corrected = (await corrections.ListForDeliveryAsync(delivery.Id, cancellationToken))
            .SelectMany(c => c.Lines)
            .GroupBy(l => l.DeliveryLineId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.QuantityDelta));

        var now = clock.GetUtcNow();
        var created = DeliveryCorrection.Create(
            Guid.CreateVersion7(),
            delivery,
            input.NoteNo,
            input.NoteDate,
            input.Reason,
            input.Lines.Select(l => (l.DeliveryLineId, l.QuantityDelta)).ToList(),
            corrected,
            currentUser.UserId,
            now);
        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        var correction = created.Value;
        var moved = await stock.CorrectAsync(
            new StockCorrection(
                DocumentType,
                correction.Id,
                currentUser.UserId,
                correction.Lines.Select(l => new StockCorrectionLine(l.DeliveryLineId, l.BaseQuantityDelta, l.DeliveryLinePosition)).ToList()),
            cancellationToken);
        if (moved.IsFailure)
        {
            return Result.Failure<Guid>(moved.Error);
        }

        corrections.Add(correction);
        var saved = await corrections.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? Result.Failure<Guid>(saved.Error) : Result.Success(correction.Id);
    }
}
