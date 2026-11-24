using Apteka.Infrastructure.Common;
using Apteka.Infrastructure.Common.Persistence;
using Apteka.Inventory.Application.Issuing;
using Apteka.Inventory.Application.Receiving;
using Apteka.Inventory.Contracts;
using Apteka.Inventory.Infrastructure.Batches;
using Apteka.Inventory.Infrastructure.Persistence;
using Apteka.Inventory.Infrastructure.StockMovements;
using Apteka.Inventory.Application.Stocktakings;
using Apteka.Inventory.Application.WriteOffs;
using Apteka.Inventory.Infrastructure.Stocktakings;
using Apteka.Inventory.Infrastructure.WriteOffs;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Inventory.Infrastructure;

public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, string connectionString)
    {
        services.AddModuleDbContext<InventoryDbContext>(connectionString, InventorySchema.Name);
        services.AddScoped<IModuleDatabaseInitializer, InventoryDatabaseInitializer>();

        // Infrastructure
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IStockLocking, StockLocking>();
        services.AddScoped<IWriteOffRepository, WriteOffRepository>();
        services.AddScoped<IStocktakingRepository, StocktakingRepository>();

        // Use cases
        services.AddScoped<CreateWriteOffHandler>();
        services.AddScoped<ListWriteOffsHandler>();
        services.AddScoped<GetWriteOffHandler>();
        services.AddScoped<StartStocktakingHandler>();
        services.AddScoped<CountStocktakingHandler>();
        services.AddScoped<CloseStocktakingHandler>();
        services.AddScoped<ListStocktakingsHandler>();
        services.AddScoped<GetStocktakingHandler>();

        // Public API for other modules
        services.AddScoped<IStockReceiving, ReceiveStockHandler>();
        services.AddScoped<IStockQueries, StockQueries>();
        services.AddScoped<IStockIssuing, IssueStockHandler>();

        return services;
    }
}
