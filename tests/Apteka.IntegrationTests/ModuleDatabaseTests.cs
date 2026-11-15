using Apteka.Audit.Infrastructure;
using Apteka.Catalog.Infrastructure;
using Apteka.Identity.Infrastructure;
using Apteka.Inventory.Infrastructure;
using Apteka.Purchasing.Infrastructure;
using Apteka.Sales.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class ModuleDatabaseTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Every_module_context_can_connect_to_PostgreSQL()
    {
        var services = new ServiceCollection()
            .AddCatalogModule(postgres.ConnectionString)
            .AddPurchasingModule(postgres.ConnectionString)
            .AddInventoryModule(postgres.ConnectionString)
            .AddSalesModule(postgres.ConnectionString)
            .AddIdentityModule(postgres.ConnectionString)
            .AddAuditModule(postgres.ConnectionString);

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        DbContext[] contexts =
        [
            scope.ServiceProvider.GetRequiredService<CatalogDbContext>(),
            scope.ServiceProvider.GetRequiredService<PurchasingDbContext>(),
            scope.ServiceProvider.GetRequiredService<InventoryDbContext>(),
            scope.ServiceProvider.GetRequiredService<SalesDbContext>(),
            scope.ServiceProvider.GetRequiredService<IdentityDbContext>(),
            scope.ServiceProvider.GetRequiredService<AuditDbContext>(),
        ];

        foreach (var context in contexts)
        {
            Assert.True(await context.Database.CanConnectAsync(TestContext.Current.CancellationToken), $"{context.GetType().Name} cannot connect.");
        }
    }
}
