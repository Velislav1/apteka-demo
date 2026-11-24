using Apteka.Catalog.Domain.Common;
using Apteka.SharedKernel;

namespace Apteka.Catalog.Domain.Shelves;

/// <summary>Where a product is kept in the pharmacy (a shelf, the fridge, the safe).</summary>
public sealed class Shelf : CatalogReference
{
    private Shelf()
    {
    }

    private Shelf(Guid id)
        : base(id)
    {
    }

    public static Result<Shelf> Create(string name) => Create(new Shelf(Guid.CreateVersion7()), name);
}
