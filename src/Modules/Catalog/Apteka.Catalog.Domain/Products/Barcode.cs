using Apteka.SharedKernel;

namespace Apteka.Catalog.Domain.Products;

/// <summary>A barcode that identifies one unit of a product. <see cref="Code"/> is unique across all products.</summary>
public sealed class Barcode : Entity<Guid>
{
    private Barcode()
    {
        Code = string.Empty;
    }

    internal Barcode(Guid productId, Guid productUnitId, string code)
        : base(Guid.CreateVersion7())
    {
        ProductId = productId;
        ProductUnitId = productUnitId;
        Code = code;
    }

    public Guid ProductId { get; private set; }

    public Guid ProductUnitId { get; private set; }

    public string Code { get; private set; }

    internal void Change(string code, Guid productUnitId)
    {
        Code = code;
        ProductUnitId = productUnitId;
    }
}
