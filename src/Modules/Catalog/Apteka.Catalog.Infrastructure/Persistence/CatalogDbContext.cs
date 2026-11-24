using Apteka.Catalog.Domain.Products;
using Apteka.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Catalog.Infrastructure;

public static class CatalogSchema
{
    public const string Name = "catalog";
}

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : ModuleDbContext(options, CatalogSchema.Name)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductUnit> ProductUnits => Set<ProductUnit>();

    public DbSet<Barcode> Barcodes => Set<Barcode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
        UseSnakeCaseNames(modelBuilder);
    }
}
