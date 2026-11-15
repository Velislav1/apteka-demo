using Apteka.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Catalog.Tests;

public class CatalogModuleTests
{
    [Fact]
    public void AddCatalogModule_registers_a_db_context_in_its_own_schema()
    {
        var services = new ServiceCollection().AddCatalogModule("Host=localhost;Database=unused");
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        Assert.Equal("catalog", context.Model.GetDefaultSchema());
    }
}