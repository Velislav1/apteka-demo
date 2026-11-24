using Apteka.Inventory.Domain.Stocktakings;
using Apteka.SharedKernel;

namespace Apteka.Inventory.Application.Stocktakings;

public interface IStocktakingRepository
{
    void Add(Stocktaking stocktaking);

    /// <summary>With its lines, tracked for changes.</summary>
    Task<Stocktaking?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> AnyOpenAsync(CancellationToken cancellationToken);

    /// <summary>The number of an open stocktaking that counts any of these products, or null.</summary>
    Task<long?> FindOpenForProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);

    /// <summary>The number of an open stocktaking that counts any of these batches' products, or null.</summary>
    Task<long?> FindOpenForBatchesAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<StocktakingListItem>> ListAsync(int limit, CancellationToken cancellationToken);

    /// <summary>Saves; a second open stocktaking started at the same moment is reported as an error.</summary>
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record StocktakingListItem(
    Guid Id,
    long Number,
    string Scope,
    StocktakingStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? ClosedAt,
    int LineCount,
    int CountedCount,
    decimal DifferenceValue);
