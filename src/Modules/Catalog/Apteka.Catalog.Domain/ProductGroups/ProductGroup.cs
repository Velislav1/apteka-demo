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
}
