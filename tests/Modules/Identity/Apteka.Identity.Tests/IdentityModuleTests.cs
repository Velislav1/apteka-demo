using Apteka.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Identity.Tests;

public class IdentityModuleTests
{
    [Fact]
    public void AddIdentityModule_registers_a_db_context_in_its_own_schema()
    {
        var services = new ServiceCollection().AddIdentityModule("Host=localhost;Database=unused");
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        Assert.Equal("identity", context.Model.GetDefaultSchema());
    }
}