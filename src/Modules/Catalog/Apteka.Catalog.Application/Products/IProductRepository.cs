using Apteka.Catalog.Domain.Products;
using Apteka.SharedKernel;

namespace Apteka.Catalog.Application.Products;

/// <summary>Loads and saves whole products (with units and barcodes). Implemented in Infrastructure.</summary>
public interface IProductRepository
{
    Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The codes from <paramref name="codes"/> already used by a product other than <paramref name="exceptProductId"/>.</summary>
    Task<IReadOnlyList<string>> FindTakenBarcodesAsync(IEnumerable<string> codes, Guid? exceptProductId, CancellationToken cancellationToken);

    void Add(Product product);

    /// <summary>Saves; a barcode taken by a concurrent save is reported as an error, not an exception.</summary>
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}
