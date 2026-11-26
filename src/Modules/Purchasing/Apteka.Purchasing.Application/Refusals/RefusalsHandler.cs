using Apteka.Purchasing.Domain.Refusals;
using Apteka.SharedKernel;

namespace Apteka.Purchasing.Application.Refusals;

public interface IRefusalRepository
{
    void Add(Refusal refusal);

    /// <summary>Created in [fromUtc, toUtc), newest first.</summary>
    Task<IReadOnlyList<Refusal>> ListAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>„Откази“: record what was asked for and missing; list them for a period.</summary>
public sealed class RefusalsHandler(IRefusalRepository refusals, IUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider clock)
{
    public Task<Result<Guid>> AddAsync(Guid? productId, string? text, int baseQuantity, string? note, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var created = Refusal.Create(productId, text, baseQuantity, note, currentUser.UserId, clock.GetUtcNow());
            if (created.IsFailure)
            {
                return Result.Failure<Guid>(created.Error);
            }

            refusals.Add(created.Value);
            await refusals.SaveChangesAsync(ct);
            return Result.Success(created.Value.Id);
        }, cancellationToken);

    public Task<IReadOnlyList<Refusal>> ListAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        refusals.ListAsync(BulgarianTime.StartOfDayUtc(from), BulgarianTime.StartOfDayUtc(to.AddDays(1)), cancellationToken);

    /// <summary>Refused quantity per product (base units) in the last <paramref name="days"/> days, for the order proposal.</summary>
    public async Task<IReadOnlyDictionary<Guid, int>> RefusedAsync(int days, CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        return (await refusals.ListAsync(now.AddDays(-days), now, cancellationToken))
            .Where(r => r.ProductId is not null)
            .GroupBy(r => r.ProductId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Quantity));
    }
}
