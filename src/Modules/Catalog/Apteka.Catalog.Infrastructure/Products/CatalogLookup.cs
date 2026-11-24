using Apteka.Catalog.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Catalog.Infrastructure.Products;

internal sealed class CatalogLookup(CatalogDbContext context) : ICatalogLookup
{
    public async Task<CatalogUnitInfo?> FindByBarcodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var trimmed = code.Trim();

        return await (
                from barcode in context.Barcodes.AsNoTracking()
                where barcode.Code == trimmed
                join unit in context.ProductUnits on barcode.ProductUnitId equals unit.Id
                join product in context.Products on barcode.ProductId equals product.Id
                select new CatalogUnitInfo(
                    product.Id,
                    product.Name,
                    product.VatRate,
                    product.IsRx,
                    product.IsActive,
                    unit.Id,
                    unit.Name,
                    unit.Factor))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CatalogProductInfo?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default) =>
        await context.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new CatalogProductInfo(
                p.Id,
                p.Name,
                p.VatRate,
                p.IsRx,
                p.IsActive,
                p.Units.OrderBy(u => u.Factor).Select(u => new CatalogUnit(u.Id, u.Name, u.Factor)).ToList()))
            .SingleOrDefaultAsync(cancellationToken);
}
