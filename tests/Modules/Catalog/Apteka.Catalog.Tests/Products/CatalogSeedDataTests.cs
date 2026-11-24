using Apteka.Catalog.Domain.Products;
using Apteka.Catalog.Infrastructure.Seed;

namespace Apteka.Catalog.Tests.Products;

public class CatalogSeedDataTests
{
    [Fact]
    public void Seed_has_50_valid_products_with_unique_in_store_barcodes()
    {
        var products = CatalogSeedData.Products()
            .Select(p => Product.Create(p.Details, p.Units, p.Barcodes))
            .ToList();

        Assert.Equal(50, products.Count);
        Assert.All(products, p => Assert.True(p.IsSuccess, p.IsFailure ? p.Error.Message : null));

        var codes = products.SelectMany(p => p.Value.Barcodes).Select(b => b.Code).ToList();
        Assert.Equal(codes.Count, codes.Distinct().Count());
        Assert.All(codes, c => Assert.StartsWith("200", c, StringComparison.Ordinal));
        Assert.Equal(50, products.Select(p => p.Value.Name).Distinct().Count());
    }
}
