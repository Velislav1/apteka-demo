using Apteka.Catalog.Application.Common;
using Apteka.Catalog.Domain.Common;
using Apteka.Catalog.Domain.ProductGroups;
using Apteka.SharedKernel;

namespace Apteka.Catalog.Application.ProductGroups;

/// <summary>Use cases of the „Групи“ list, including the group's trade markup.</summary>
public sealed class ProductGroupsHandler(IReferenceRepository<ProductGroup> items, IUnitOfWork unitOfWork)
    : ReferenceDataHandler<ProductGroup>(items, ProductGroup.Create, unitOfWork)
{
    private readonly IReferenceRepository<ProductGroup> _items = items;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    /// <summary>The markup of every group that has one.</summary>
    public async Task<IReadOnlyDictionary<Guid, decimal>> GetMarkupsAsync(CancellationToken cancellationToken = default)
    {
        var groups = await _items.ListAsync(includeInactive: true, cancellationToken);
        var result = new Dictionary<Guid, decimal>();
        foreach (var group in groups)
        {
            if (await _items.GetAsync(group.Id, cancellationToken) is { MarkupPercent: { } markup })
            {
                result[group.Id] = markup;
            }
        }

        return result;
    }

    public Task<Result> SetMarkupAsync(Guid id, decimal? markupPercent, CancellationToken cancellationToken = default) =>
        _unitOfWork.ExecuteAsync(
            async ct =>
            {
                var group = await _items.GetAsync(id, ct);
                if (group is null)
                {
                    return Result.Failure(ReferenceErrors.NotFound);
                }

                var set = group.SetMarkup(markupPercent);
                return set.IsFailure ? set : await _items.SaveChangesAsync(ct);
            },
            cancellationToken);
}
