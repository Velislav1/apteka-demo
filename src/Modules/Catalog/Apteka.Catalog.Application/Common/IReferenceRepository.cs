using Apteka.Catalog.Domain.Common;
using Apteka.SharedKernel;

namespace Apteka.Catalog.Application.Common;

/// <summary>Storage of one kind of catalog reference item (manufacturers, groups, shelves). Implemented in Infrastructure.</summary>
public interface IReferenceRepository<T>
    where T : CatalogReference
{
    Task<IReadOnlyList<ReferenceItemView>> ListAsync(bool includeInactive, CancellationToken cancellationToken);

    Task<T?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>True when another item already has this name (ignoring case).</summary>
    Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken);

    void Add(T item);

    /// <summary>Saves; a name taken by a concurrent save is reported as an error, not an exception.</summary>
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record ReferenceItemView(Guid Id, string Name, bool IsActive);
