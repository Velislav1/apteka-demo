using Apteka.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Apteka.IntegrationTests.Catalog;

[Collection(PostgresCollection.Name)]
public class CatalogPageTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Development_startup_migrates_and_seeds_so_the_catalog_page_lists_demo_products()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Apteka", postgres.ConnectionString);

        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/catalog", TestContext.Current.CancellationToken);

        Assert.Contains("Номенклатура", html);
        Assert.Contains("Амброксол 30 мг таблетки", html);
        Assert.Contains("Парацетамол 500 мг таблетки", html);
    }
}
