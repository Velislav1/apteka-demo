using Apteka.Catalog.Application.Products;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Catalog.Infrastructure.Products;

internal sealed class ProductQueries(CatalogDbContext context) : IProductQueries
{
    public async Task<IReadOnlyList<ProductListItem>> SearchAsync(string? text, int limit, CancellationToken cancellationToken)
    {
        var products = context.Products.AsNoTracking();

        if (text is not null)
        {
            var pattern = "%" + EscapeLike(text) + "%";
            products = products.Where(p =>
                EF.Functions.ILike(p.Name, pattern, "\\")
                || p.Barcodes.Any(b => b.Code == text));
        }

        return await products
            .OrderBy(p => p.Name)
            .Take(limit)
            .Select(p => new ProductListItem(
                p.Id,
                p.Name,
                p.Manufacturer,
                p.VatRate,
                p.IsRx,
                p.IsActive,
                p.Units.Where(u => u.Factor == 1).Select(u => u.Name).FirstOrDefault() ?? string.Empty,
                p.Barcodes.OrderBy(b => b.Code).Select(b => b.Code).ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductView?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await context.Products
            .AsNoTracking()
            .Include(p => p.Units)
            .Include(p => p.Barcodes)
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            return null;
        }

        var units = product.Units.OrderByDescending(u => u.Factor).ToList();
        var unitNames = units.ToDictionary(u => u.Id, u => u.Name);

        return new ProductView(
            product.Id,
            product.Name,
            product.AtcCode,
            product.Manufacturer,
            product.VatRate,
            product.IsRx,
            product.IsActive,
            units.Select(u => new ProductUnitView(u.Id, u.Name, u.Factor)).ToList(),
            product.Barcodes
                .OrderBy(b => b.Code)
                .Select(b => new BarcodeView(b.Id, b.Code, b.ProductUnitId, unitNames[b.ProductUnitId]))
                .ToList());
    }

    private static string EscapeLike(string text) =>
        text.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
