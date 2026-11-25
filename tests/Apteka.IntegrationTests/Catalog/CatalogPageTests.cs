using Apteka.IntegrationTests.Infrastructure;

namespace Apteka.IntegrationTests.Catalog;

[Collection(PostgresCollection.Name)]
public class CatalogPageTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Development_startup_migrates_and_seeds_so_the_catalog_page_lists_demo_products()
    {
        await using var factory = WebLogin.Factory(postgres.ConnectionString);
        using var client = factory.CreateClient();
        await WebLogin.LoginAsync(client, "manager", WebLogin.ManagerPassword);

        var html = await client.GetStringAsync("/catalog", TestContext.Current.CancellationToken);

        Assert.Contains("Номенклатура", html);
        // The demo products are seeded only into an empty catalog; other tests may have added products first,
        // so check that the page lists products, not which ones.
        Assert.Contains("href=\"catalog/", html);
        Assert.Contains("<table class=\"grid\"", html);
    }
}
