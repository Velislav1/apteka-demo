using Apteka.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Inventory.Tests;

public class InventoryModuleTests
{
    [Fact]
    public void AddInventoryModule_registers_a_db_context_in_its_own_schema()
    {
        var services = new ServiceCollection().AddInventoryModule("Host=localhost;Database=unused");
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        Assert.Equal("inventory", context.Model.GetDefaultSchema());
    }
}