using Apteka.Audit.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Audit.Tests;

public class AuditModuleTests
{
    [Fact]
    public void AddAuditModule_registers_a_db_context_in_its_own_schema()
    {
        var services = new ServiceCollection().AddAuditModule("Host=localhost;Database=unused");
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

        Assert.Equal("audit", context.Model.GetDefaultSchema());
    }
}