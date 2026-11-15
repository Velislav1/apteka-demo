using Apteka.Audit.Infrastructure;
using Apteka.Catalog.Infrastructure;
using Apteka.Identity.Infrastructure;
using Apteka.Inventory.Infrastructure;
using Apteka.Purchasing.Infrastructure;
using Apteka.Sales.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.ArchitectureTests;

/// <summary>Every module owns one PostgreSQL schema, used for its tables and for its migration history.</summary>
public class ModuleSchemaRulesTests
{
    private const string ConnectionString = "Host=localhost;Database=unused";

    private static readonly (string Module, string ExpectedSchema, string ActualSchema, Func<IServiceCollection, IServiceCollection> Register, Type Context)[] Modules =
    [
        ("Catalog", "catalog", CatalogSchema.Name, s => s.AddCatalogModule(ConnectionString), typeof(CatalogDbContext)),
        ("Purchasing", "purchasing", PurchasingSchema.Name, s => s.AddPurchasingModule(ConnectionString), typeof(PurchasingDbContext)),
        ("Inventory", "inventory", InventorySchema.Name, s => s.AddInventoryModule(ConnectionString), typeof(InventoryDbContext)),
        ("Sales", "sales", SalesSchema.Name, s => s.AddSalesModule(ConnectionString), typeof(SalesDbContext)),
        ("Identity", "identity", IdentitySchema.Name, s => s.AddIdentityModule(ConnectionString), typeof(IdentityDbContext)),
        ("Audit", "audit", AuditSchema.Name, s => s.AddAuditModule(ConnectionString), typeof(AuditDbContext)),
    ];

    [Fact]
    public void Every_known_module_is_covered_by_this_test()
    {
        Assert.Equal(RepositoryLayout.Modules.Order(), Modules.Select(m => m.Module).Order());
    }

    [Fact]
    public void Each_module_has_its_own_schema()
    {
        Assert.Equal(Modules.Length, Modules.Select(m => m.ActualSchema).Distinct().Count());

        foreach (var module in Modules)
        {
            Assert.Equal(module.ExpectedSchema, module.ActualSchema);
        }
    }

    [Fact]
    public void Each_module_context_uses_its_schema_for_tables_and_migration_history()
    {
        foreach (var module in Modules)
        {
            var services = module.Register(new ServiceCollection());
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var context = (DbContext)scope.ServiceProvider.GetRequiredService(module.Context);

            Assert.Equal(module.ExpectedSchema, context.Model.GetDefaultSchema());

            var relational = context.GetService<IDbContextOptions>().Extensions.OfType<RelationalOptionsExtension>().Single();
            Assert.Equal(module.ExpectedSchema, relational.MigrationsHistoryTableSchema);
        }
    }
}
