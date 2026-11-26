using Apteka.Infrastructure.Common;
using Apteka.Infrastructure.Common.Persistence;
using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Application.Common;
using Apteka.Sales.Application.Customers;
using Apteka.Sales.Infrastructure.Customers;
using Apteka.Sales.Application.DailySales;
using Apteka.Sales.Application.Returns;
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
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<Apteka.Sales.Application.Shifts.IShiftRepository, Shifts.ShiftRepository>();
        services.AddScoped<Apteka.Sales.Application.Receivables.IReceivablesRepository, Receivables.ReceivablesRepository>();

        // Use cases
        services.AddScoped<FindTillProductHandler>();
        services.AddScoped<CompleteSaleHandler>();
        services.AddScoped<ReverseSaleHandler>();
        services.AddScoped<DailySalesHandler>();
        services.AddScoped<DailyReportHandler>();
        services.AddScoped<GetSaleHandler>();
        services.AddScoped<CustomersHandler>();
        services.AddScoped<CreateReturnHandler>();
        services.AddScoped<FindSaleHandler>();
        services.AddScoped<Apteka.Sales.Application.BatchBuyers.BatchBuyersHandler>();
        services.AddScoped<Apteka.Sales.Application.Statistics.SalesStatisticsHandler>();
        services.AddScoped<Apteka.Sales.Application.Shifts.ShiftsHandler>();
        services.AddScoped<Apteka.Sales.Application.Receivables.ReceivablesHandler>();

        return services;
    }
}
