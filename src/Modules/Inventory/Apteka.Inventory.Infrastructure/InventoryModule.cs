using Apteka.Infrastructure.Common;
using Apteka.Infrastructure.Common.Persistence;
using Apteka.Inventory.Application.Issuing;
using Apteka.Inventory.Application.Receiving;
using Apteka.Inventory.Contracts;
using Apteka.Inventory.Infrastructure.Batches;
using Apteka.Inventory.Infrastructure.Persistence;
using Apteka.Inventory.Infrastructure.StockMovements;
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

        // Public API for other modules
        services.AddScoped<IStockReceiving, ReceiveStockHandler>();
        services.AddScoped<IStockQueries, StockQueries>();
        services.AddScoped<IStockIssuing, IssueStockHandler>();

        return services;
    }
}
