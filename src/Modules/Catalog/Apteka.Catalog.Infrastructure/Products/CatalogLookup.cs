using Apteka.Catalog.Contracts;
using Apteka.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Catalog.Infrastructure.Products;

internal sealed class CatalogLookup(CatalogDbContext context) : ICatalogLookup
{
    public async Task<CatalogUnitInfo?> FindByBarcodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var trimmed = code.Trim();

        var row = await (
                from barcode in context.Barcodes.AsNoTracking()
                where barcode.Code == trimmed
                join unit in context.ProductUnits on barcode.ProductUnitId equals unit.Id
                join product in context.Products on barcode.ProductId equals product.Id
                select new { product.Id, product.Name, product.VatRate, product.DispensingMode, product.IsActive, UnitId = unit.Id, UnitName = unit.Name, unit.Factor })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new CatalogUnitInfo(row.Id, row.Name, row.VatRate, ToContract(row.DispensingMode), row.IsActive, row.UnitId, row.UnitName, row.Factor);
    }

    public async Task<CatalogProductInfo?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await context.Products
            .AsNoTracking()
            .Include(p => p.Units)
            .SingleOrDefaultAsync(p => p.Id == productId, cancellationToken);

        return product is null
            ? null
            : new CatalogProductInfo(
                product.Id,
                product.Name,
                product.VatRate,
                ToContract(product.DispensingMode),
                product.IsActive,
                product.DefaultSaleUnitId,
                product.Units.OrderBy(u => u.Factor).Select(u => new CatalogUnit(u.Id, u.Name, u.Factor)).ToList());
    }

    // The values match by design (see CatalogDispensingMode); mapping by name keeps it safe if one side changes.
    private static CatalogDispensingMode ToContract(DispensingMode mode) => Enum.Parse<CatalogDispensingMode>(mode.ToString());
}
