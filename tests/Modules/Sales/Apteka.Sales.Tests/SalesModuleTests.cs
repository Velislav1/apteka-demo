using Apteka.Sales.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Sales.Tests;

public class SalesModuleTests
{
    [Fact]
    public void AddSalesModule_registers_a_db_context_in_its_own_schema()
    {
        var services = new ServiceCollection().AddSalesModule("Host=localhost;Database=unused");
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        Assert.Equal("sales", context.Model.GetDefaultSchema());
    }
}