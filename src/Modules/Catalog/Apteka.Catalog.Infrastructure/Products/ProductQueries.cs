using Apteka.Catalog.Application.Products;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Catalog.Infrastructure.Products;

internal sealed class ProductQueries(CatalogDbContext context) : IProductQueries
{
    public async Task<IReadOnlyList<ProductListItem>> SearchAsync(string? text, int limit, CancellationToken cancellationToken, bool? active = null)
    {
        var products = context.Products.AsNoTracking();
        if (active is { } onlyActive)
        {
            products = products.Where(p => p.IsActive == onlyActive);
        }

        if (text is not null)
        {
            var pattern = "%" + EscapeLike(text) + "%";
            long? code = Apteka.Catalog.Contracts.CatalogCodes.LooksLikeCode(text) ? long.Parse(text, System.Globalization.CultureInfo.InvariantCulture) : null;
            products = products.Where(p =>
                EF.Functions.ILike(p.Name, pattern, "\\")
                || (p.LatinName != null && EF.Functions.ILike(p.LatinName, pattern, "\\"))
                || (p.GenericName != null && EF.Functions.ILike(p.GenericName, pattern, "\\"))
                || (p.ActiveSubstance != null && EF.Functions.ILike(p.ActiveSubstance, pattern, "\\"))
                || p.Barcodes.Any(b => b.Code == text)
                || p.Code == code);
        }

        return await products
            .OrderBy(p => p.Name)
            .Take(limit)
            .Select(p => new ProductListItem(
                p.Id,
                p.Code,
                p.Name,
                context.Manufacturers.Where(m => m.Id == p.ManufacturerId).Select(m => m.Name).FirstOrDefault(),
                context.ProductGroups.Where(g => g.Id == p.ProductGroupId).Select(g => g.Name).FirstOrDefault(),
                p.VatRate,
                p.DispensingMode,
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
            product.Code,
            product.Name,
            product.AtcCode,
            product.DispensingMode,
            product.VatRate,
            product.IsActive,
            product.NhifCode,
            product.ManufacturerId,
            product.ProductGroupId,
            product.ShelfId,
            product.DefaultSaleUnitId is { } unitId ? unitNames[unitId] : null,
            units.Select(u => new ProductUnitView(u.Id, u.Name, u.Factor)).ToList(),
            product.Barcodes
                .OrderBy(b => b.Code)
                .Select(b => new BarcodeView(b.Id, b.Code, b.ProductUnitId, unitNames[b.ProductUnitId]))
                .ToList(),
            product.LatinName,
            product.GenericName,
            product.MarkupPercent,
            product.MinStock,
            product.MaxStock,
            product.ActiveSubstance);
    }

    private static string EscapeLike(string text) =>
        text.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
