using Apteka.Audit.Infrastructure;
using Apteka.Catalog.Infrastructure;
using Apteka.Infrastructure.Common;
using Apteka.Infrastructure.Common.Users;
using Apteka.Inventory.Infrastructure;
using Apteka.Purchasing.Infrastructure;
using Apteka.Sales.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.IntegrationTests.Infrastructure;

/// <summary>The modules wired the same way as in the Host, against the test container, with migrations applied.</summary>
internal static class ModuleServices
{
    public static async Task<ServiceProvider> CreateAsync(
        string connectionString,
        ICurrentUser? user = null,
        Action<IServiceCollection>? replace = null)
    {
        var services = new ServiceCollection();
        if (user is not null)
        {
            services.AddScoped(_ => user);
        }

        services.AddAptekaCommon(connectionString);
        services.AddAuditModule(connectionString);
        services.AddCatalogModule(connectionString);
        services.AddInventoryModule(connectionString);
        services.AddPurchasingModule(connectionString);
        services.AddSalesModule(connectionString);
        replace?.Invoke(services);

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await provider.InitializeModuleDatabasesAsync(seed: false, TestContext.Current.CancellationToken);
        return provider;
    }
}

internal sealed record TestUser(Guid? UserId, string UserName) : ICurrentUser;
