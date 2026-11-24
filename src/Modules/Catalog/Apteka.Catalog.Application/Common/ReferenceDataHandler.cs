using Apteka.Catalog.Domain.Common;
using Apteka.SharedKernel;

namespace Apteka.Catalog.Application.Common;

/// <summary>List, add, rename and (de)activate items of one catalog reference list.</summary>
public abstract class ReferenceDataHandler<T>(IReferenceRepository<T> items, Func<string, Result<T>> create, IUnitOfWork unitOfWork)
    where T : CatalogReference
{
    public Task<IReadOnlyList<ReferenceItemView>> ListAsync(bool includeInactive = true, CancellationToken cancellationToken = default) =>
        items.ListAsync(includeInactive, cancellationToken);

    public async Task<Result<Guid>> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var created = create(name);
        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        var item = created.Value;
        if (await items.NameExistsAsync(item.Name, null, cancellationToken))
        {
            return Result.Failure<Guid>(ReferenceErrors.NameTaken(item.Name));
        }

        items.Add(item);
        var saved = await items.SaveChangesAsync(cancellationToken);
        return saved.IsSuccess ? Result.Success(item.Id) : Result.Failure<Guid>(saved.Error);
    }

    /// <summary>Runs in <see cref="IUnitOfWork"/>: a refused rename is rolled back and forgotten.</summary>
    public Task<Result> UpdateAsync(Guid id, string name, bool isActive, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(ct => UpdateCoreAsync(id, name, isActive, ct), cancellationToken);

    private async Task<Result> UpdateCoreAsync(Guid id, string name, bool isActive, CancellationToken cancellationToken)
    {
        var item = await items.GetAsync(id, cancellationToken);
        if (item is null)
        {
            return Result.Failure(ReferenceErrors.NotFound);
        }

        var updated = item.Update(name, isActive);
        if (updated.IsFailure)
        {
            return updated;
        }

        if (await items.NameExistsAsync(item.Name, item.Id, cancellationToken))
        {
            return Result.Failure(ReferenceErrors.NameTaken(item.Name));
        }

        return await items.SaveChangesAsync(cancellationToken);
    }
}
