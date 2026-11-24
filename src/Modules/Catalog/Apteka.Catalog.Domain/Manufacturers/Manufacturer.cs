using Apteka.Catalog.Domain.Common;
using Apteka.SharedKernel;

namespace Apteka.Catalog.Domain.Manufacturers;

/// <summary>The company that makes a product.</summary>
public sealed class Manufacturer : CatalogReference
{
    private Manufacturer()
    {
    }

    private Manufacturer(Guid id)
        : base(id)
    {
    }

    public static Result<Manufacturer> Create(string name) => Create(new Manufacturer(Guid.CreateVersion7()), name);
}
