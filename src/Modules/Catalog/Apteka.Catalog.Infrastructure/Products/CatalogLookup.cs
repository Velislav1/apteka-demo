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

    public async Task<CatalogProductInfo?> FindByCodeAsync(long code, CancellationToken cancellationToken = default)
    {
        var products = await context.Products.AsNoTracking().Include(p => p.Units).Where(p => p.Code == code).ToListAsync(cancellationToken);
        return (await ToInfoAsync(products, cancellationToken)).SingleOrDefault();
    }

    public async Task<CatalogProductInfo?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default) =>
        (await GetProductsAsync([productId], cancellationToken)).SingleOrDefault();

    public async Task<IReadOnlyList<CatalogProductInfo>> GetProductsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        var products = await context.Products
            .AsNoTracking()
            .Include(p => p.Units)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        return await ToInfoAsync(products, cancellationToken);
    }

    public async Task<IReadOnlyList<CatalogProductInfo>> SearchProductsAsync(
        string text,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var pattern = "%" + text.Trim().Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal) + "%";

        var products = await context.Products
            .AsNoTracking()
            .Include(p => p.Units)
            .Where(p => p.IsActive && EF.Functions.ILike(p.Name, pattern, "\\"))
            .OrderBy(p => p.Name)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return await ToInfoAsync(products, cancellationToken);
    }

    public async Task<IReadOnlyList<CatalogProductInfo>> ListProductsAsync(
        Guid? groupId,
        bool withMinStockOnly,
        CancellationToken cancellationToken = default,
        Guid? shelfId = null)
    {
        var query = context.Products.AsNoTracking().Include(p => p.Units).Where(p => p.IsActive);
        if (groupId is { } group)
        {
            query = query.Where(p => p.ProductGroupId == group);
        }

        if (shelfId is { } shelf)
        {
            query = query.Where(p => p.ShelfId == shelf);
        }

        if (withMinStockOnly)
        {
            query = query.Where(p => p.MinStock != null);
        }

        return await ToInfoAsync(await query.OrderBy(p => p.Name).ToListAsync(cancellationToken), cancellationToken);
    }

    public async Task<IReadOnlyList<CatalogGroup>> ListGroupsAsync(CancellationToken cancellationToken = default) =>
        await context.ProductGroups.AsNoTracking()
            .Where(g => g.IsActive)
            .OrderBy(g => g.Name)
            .Select(g => new CatalogGroup(g.Id, g.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CatalogShelf>> ListShelvesAsync(CancellationToken cancellationToken = default) =>
        await context.Shelves.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new CatalogShelf(s.Id, s.Name))
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<CatalogProductInfo>> ToInfoAsync(List<Product> products, CancellationToken cancellationToken)
    {
        var shelfIds = products.Where(p => p.ShelfId is not null).Select(p => p.ShelfId!.Value).Distinct().ToArray();
        var shelves = await context.Shelves.AsNoTracking()
            .Where(s => shelfIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);
        var groupIds = products.Where(p => p.ProductGroupId is not null).Select(p => p.ProductGroupId!.Value).Distinct().ToArray();
        var groupMarkups = await context.ProductGroups.AsNoTracking()
            .Where(g => groupIds.Contains(g.Id) && g.MarkupPercent != null)
            .ToDictionaryAsync(g => g.Id, g => g.MarkupPercent!.Value, cancellationToken);

        return products.Select(p => ToInfo(
            p,
            p.ShelfId is { } id ? shelves.GetValueOrDefault(id) : null,
            EffectiveMarkup(p, groupMarkups))).ToList();
    }

    /// <summary>As in Apteka Expert 2: the product's markup, else its group's, else the default.</summary>
    private static decimal EffectiveMarkup(Product product, Dictionary<Guid, decimal> groupMarkups) =>
        product.MarkupPercent
        ?? (product.ProductGroupId is { } groupId && groupMarkups.TryGetValue(groupId, out var groupMarkup) ? groupMarkup : CatalogErrors.DefaultMarkupPercent);

    private static CatalogProductInfo ToInfo(Product product, string? shelfName, decimal markupPercent) => new(
        product.Id,
        product.Name,
        product.VatRate,
        ToContract(product.DispensingMode),
        product.IsActive,
        product.DefaultSaleUnitId,
        product.Units.OrderBy(u => u.Factor).Select(u => new CatalogUnit(u.Id, u.Name, u.Factor)).ToList(),
        shelfName,
        product.Code,
        markupPercent,
        product.ProductGroupId,
        product.MinStock,
        product.MaxStock);

    // The values match by design (see CatalogDispensingMode); mapping by name keeps it safe if one side changes.
    private static CatalogDispensingMode ToContract(DispensingMode mode) => Enum.Parse<CatalogDispensingMode>(mode.ToString());
}
