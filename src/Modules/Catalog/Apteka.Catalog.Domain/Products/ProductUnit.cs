using Apteka.SharedKernel;

namespace Apteka.Catalog.Domain.Products;

/// <summary>A unit of a product (box, blister, tablet). <see cref="Factor"/> is the number of base units in it.</summary>
public sealed class ProductUnit : Entity<Guid>
{
    private ProductUnit()
    {
        Name = string.Empty;
    }

    internal ProductUnit(Guid productId, string name, int factor)
        : base(Guid.CreateVersion7())
    {
        ProductId = productId;
        Name = name;
        Factor = factor;
    }

    public Guid ProductId { get; private set; }

    public string Name { get; private set; }

    /// <summary>Fixed once created: stock is kept in base units and older documents rely on it.</summary>
    public int Factor { get; private set; }

    public bool IsBaseUnit => Factor == 1;

    /// <summary>Converts a count of this unit to base units.</summary>
    public Quantity ToBaseUnits(int count) => Quantity.FromUnits(count, Factor);

    internal void Rename(string name) => Name = name;
}
