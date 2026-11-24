using Apteka.Infrastructure.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Sales.Infrastructure;

public static class SalesModule
{
    public static IServiceCollection AddSalesModule(this IServiceCollection services, string connectionString)
    {
        services.AddModuleDbContext<SalesDbContext>(connectionString, SalesSchema.Name);

        return services;
    }
}
