using Apteka.Infrastructure.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Inventory.Infrastructure;

public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, string connectionString)
    {
        services.AddModuleDbContext<InventoryDbContext>(connectionString, InventorySchema.Name);

        return services;
    }
}
