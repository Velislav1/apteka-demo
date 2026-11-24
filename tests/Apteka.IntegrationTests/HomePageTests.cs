using Microsoft.AspNetCore.Mvc.Testing;

using Apteka.IntegrationTests.Infrastructure;

namespace Apteka.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class HomePageTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Home_page_is_in_Bulgarian_and_reports_a_working_database_connection()
    {
        // WebApplication.CreateBuilder reads environment variables, which is the reliable way to feed Program.cs.
        Environment.SetEnvironmentVariable("ConnectionStrings__Apteka", postgres.ConnectionString);

        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/", TestContext.Current.CancellationToken);

        Assert.Contains("<html lang=\"bg\">", html);
        Assert.Contains("Аптека", html);
        Assert.Contains("налична", html);
    }
}
