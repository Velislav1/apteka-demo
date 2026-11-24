using Apteka.Catalog.Application.Products;
using Apteka.Catalog.Contracts;
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

        // Use cases
        services.AddScoped<CreateProductHandler>();
        services.AddScoped<UpdateProductHandler>();
        services.AddScoped<SearchProductsHandler>();
        services.AddScoped<GetProductHandler>();

        // Public API for other modules
        services.AddScoped<ICatalogLookup, CatalogLookup>();

        return services;
    }
}
