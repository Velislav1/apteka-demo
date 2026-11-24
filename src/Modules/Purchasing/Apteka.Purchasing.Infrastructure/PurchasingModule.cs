using Apteka.Infrastructure.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Purchasing.Infrastructure;

public static class PurchasingModule
{
    public static IServiceCollection AddPurchasingModule(this IServiceCollection services, string connectionString)
    {
        services.AddModuleDbContext<PurchasingDbContext>(connectionString, PurchasingSchema.Name);

        return services;
    }
}
