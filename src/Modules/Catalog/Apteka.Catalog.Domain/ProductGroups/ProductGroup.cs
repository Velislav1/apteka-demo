using Apteka.Catalog.Domain.Common;
using Apteka.SharedKernel;

namespace Apteka.Catalog.Domain.ProductGroups;

/// <summary>A product group (e.g. cardiovascular system), used for search and reports.</summary>
public sealed class ProductGroup : CatalogReference
{
    private ProductGroup()
    {
    }

    private ProductGroup(Guid id)
        : base(id)
    {
    }

    public static Result<ProductGroup> Create(string name) => Create(new ProductGroup(Guid.CreateVersion7()), name);

    /// <summary>Trade markup for the group's products that have none of their own; null means the default.</summary>
    public decimal? MarkupPercent { get; private set; }

    public Result SetMarkup(decimal? markupPercent)
    {
        if (markupPercent is < 0 or > Products.CatalogErrors.MaxMarkupPercent)
        {
            return Result.Failure(Products.CatalogErrors.InvalidMarkup);
        }

        MarkupPercent = markupPercent;
        return Result.Success();
    }
}
