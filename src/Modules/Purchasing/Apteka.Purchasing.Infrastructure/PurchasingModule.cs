using Apteka.Infrastructure.Common;
using Apteka.Infrastructure.Common.Persistence;
using Apteka.Purchasing.Application.Corrections;
using Apteka.Purchasing.Application.Deliveries;
using Apteka.Purchasing.Infrastructure.Corrections;
using Apteka.Purchasing.Application.Suppliers;
using Apteka.Purchasing.Infrastructure.Deliveries;
using Apteka.Purchasing.Infrastructure.Seed;
using Apteka.Purchasing.Infrastructure.Suppliers;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Purchasing.Infrastructure;

public static class PurchasingModule
{
    public static IServiceCollection AddPurchasingModule(this IServiceCollection services, string connectionString)
    {
        services.AddModuleDbContext<PurchasingDbContext>(connectionString, PurchasingSchema.Name);
        services.AddScoped<IModuleDatabaseInitializer, PurchasingSeeder>();

        // Infrastructure
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IDeliveryRepository, DeliveryRepository>();
        services.AddScoped<IDeliveryCorrectionRepository, DeliveryCorrectionRepository>();

        // Use cases
        services.AddScoped<SuppliersHandler>();
        services.AddScoped<SaveDeliveryDraftHandler>();
        services.AddScoped<PostDeliveryHandler>();
        services.AddScoped<ListDeliveriesHandler>();
        services.AddScoped<GetDeliveryHandler>();
        services.AddScoped<CreateCorrectionHandler>();
        services.AddScoped<ListCorrectionsHandler>();
        services.AddScoped<DeliveryCorrectionsHandler>();
        services.AddScoped<GetCorrectionHandler>();

        return services;
    }
}
