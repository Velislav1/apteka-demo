using Apteka.Inventory.Domain.WriteOffs;

namespace Apteka.Inventory.Application.WriteOffs;

/// <summary>Stores write-offs. Implemented in Infrastructure; saved with the stock movements (one DbContext).</summary>
public interface IWriteOffRepository
{
    void Add(WriteOff writeOff);

    Task<WriteOff?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Newest first, created in [fromUtc, toUtc).</summary>
    Task<IReadOnlyList<WriteOffListItem>> ListAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken);
}

public sealed record WriteOffListItem(Guid Id, long Number, DateTimeOffset CreatedAt, Guid? UserId, string? Reason, int LineCount, decimal PurchaseTotal);
