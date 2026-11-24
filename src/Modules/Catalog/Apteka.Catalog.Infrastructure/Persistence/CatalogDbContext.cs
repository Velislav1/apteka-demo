using Apteka.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Catalog.Infrastructure;

public static class CatalogSchema
{
    public const string Name = "catalog";
}

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : ModuleDbContext(options, CatalogSchema.Name);