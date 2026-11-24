using Apteka.Infrastructure.Common;
using Apteka.Infrastructure.Common.Persistence;
using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Application.Common;
using Apteka.Sales.Application.DailySales;
using Apteka.Sales.Application.Reversals;
using Apteka.Sales.Infrastructure.Persistence;
using Apteka.Sales.Infrastructure.Sales;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Sales.Infrastructure;

public static class SalesModule
{
    public static IServiceCollection AddSalesModule(this IServiceCollection services, string connectionString)
    {
        services.AddModuleDbContext<SalesDbContext>(connectionString, SalesSchema.Name);
        services.AddScoped<IModuleDatabaseInitializer, SalesDatabaseInitializer>();

        // Infrastructure
        services.AddScoped<ISaleRepository, SaleRepository>();

        // Use cases
        services.AddScoped<FindTillProductHandler>();
        services.AddScoped<CompleteSaleHandler>();
        services.AddScoped<ReverseSaleHandler>();
        services.AddScoped<DailySalesHandler>();
        services.AddScoped<GetSaleHandler>();

        return services;
    }
}
