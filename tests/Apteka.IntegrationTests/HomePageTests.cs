using Apteka.IntegrationTests.Infrastructure;

namespace Apteka.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class HomePageTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Home_page_is_in_Bulgarian_and_reports_a_working_database_connection_after_login()
    {
        await using var factory = WebLogin.Factory(postgres.ConnectionString);
        using var client = factory.CreateClient();

        var response = await WebLogin.LoginAsync(client, "manager", WebLogin.ManagerPassword);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("<html lang=\"bg\">", html);
        Assert.Contains("Аптека", html);
        Assert.Contains("налична", html);
    }
}
