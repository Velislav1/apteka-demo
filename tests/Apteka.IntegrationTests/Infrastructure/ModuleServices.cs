using Apteka.Audit.Infrastructure;
using Apteka.Catalog.Infrastructure;
using Apteka.Infrastructure.Common;
using Apteka.Identity.Contracts;
using Apteka.Identity.Infrastructure;
using Apteka.SharedKernel;
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
        services.AddLogging();
        services.AddScoped(_ => user ?? TestUser.Manager());

        services.AddAptekaCommon(connectionString);
        services.AddAuditModule(connectionString);
        services.AddCatalogModule(connectionString);
        services.AddInventoryModule(connectionString);
        services.AddPurchasingModule(connectionString);
        services.AddSalesModule(connectionString);
        services.AddIdentityModule(connectionString);
        replace?.Invoke(services);

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await provider.InitializeModuleDatabasesAsync(seed: false, TestContext.Current.CancellationToken);
        return provider;
    }
}

internal sealed record TestUser(Guid? UserId, string UserName, string Role = Roles.Manager) : ICurrentUser
{
    public static TestUser Manager() => new(Guid.CreateVersion7(), "test-manager");

    public static TestUser Pharmacist() => new(Guid.CreateVersion7(), "test-pharmacist", Roles.Pharmacist);

    public bool IsInRole(string role) => role == Role;
}
