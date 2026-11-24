using Apteka.Catalog.Application.Common;
using Apteka.Catalog.Application.Manufacturers;
using Apteka.Catalog.Application.ProductGroups;
using Apteka.Catalog.Application.Products;
using Apteka.Catalog.Application.Shelves;
using Apteka.Catalog.Contracts;
using Apteka.Catalog.Domain.Manufacturers;
using Apteka.Catalog.Domain.ProductGroups;
using Apteka.Catalog.Domain.Shelves;
using Apteka.Catalog.Infrastructure.Common;
using Apteka.Catalog.Infrastructure.Products;
using Apteka.Catalog.Infrastructure.Seed;
using Apteka.Infrastructure.Common;
using Apteka.Infrastructure.Common.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Catalog.Infrastructure;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services, string connectionString)
    {
        services.AddModuleDbContext<CatalogDbContext>(connectionString, CatalogSchema.Name);
        services.AddScoped<IModuleDatabaseInitializer, CatalogDatabaseInitializer>();

        // Infrastructure
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductQueries, ProductQueries>();
        services.AddScoped<IReferenceRepository<Manufacturer>, ReferenceRepository<Manufacturer>>();
        services.AddScoped<IReferenceRepository<ProductGroup>, ReferenceRepository<ProductGroup>>();
        services.AddScoped<IReferenceRepository<Shelf>, ReferenceRepository<Shelf>>();

        // Use cases
        services.AddScoped<CreateProductHandler>();
        services.AddScoped<UpdateProductHandler>();
        services.AddScoped<SearchProductsHandler>();
        services.AddScoped<GetProductHandler>();
        services.AddScoped<ProductReferenceRules>();
        services.AddScoped<ManufacturersHandler>();
        services.AddScoped<ProductGroupsHandler>();
        services.AddScoped<ShelvesHandler>();

        // Public API for other modules
        services.AddScoped<ICatalogLookup, CatalogLookup>();

        return services;
    }
}
