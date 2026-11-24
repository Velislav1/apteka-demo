using Apteka.Catalog.Domain.Products;
using Apteka.IntegrationTests.Infrastructure;
using Apteka.IntegrationTests.Purchasing;
using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Application.Customers;
using Apteka.Sales.Application.DailySales;
using Apteka.Sales.Domain.Customers;
using Apteka.Sales.Domain.Sales;
using Apteka.Sales.Infrastructure;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using static Apteka.IntegrationTests.Purchasing.DeliveryFixtures;

namespace Apteka.IntegrationTests.Sales;

[Collection(PostgresCollection.Name)]
public class CustomerTests(PostgresFixture postgres)
{
    private static int _next = Random.Shared.Next(100, 900);

    /// <summary>An invented, valid ЕГН that no other test uses.</summary>
    private static string NewEgn() => Egn.Complete($"6{Interlocked.Increment(ref _next) % 10}0101{Interlocked.Increment(ref _next) % 1000:D3}");

    [Fact]
    public async Task Customers_are_found_by_name_egn_code_and_phone_and_the_egn_is_unique()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        await using var scope = provider.CreateAsyncScope();
        var customers = scope.ServiceProvider.GetRequiredService<CustomersHandler>();
        var name = Apteka.IntegrationTests.Catalog.CatalogInputs.UniqueName("Демо Клиент");
        var egn = NewEgn();
        var phone = $"0899 {Random.Shared.Next(100000, 999999)}";

        var id = (await customers.CreateAsync(new CustomerDetails(name, egn, phone, null), Ct)).Value;
        var code = (await customers.GetAsync(id, Ct))!.Code;

        Assert.Equal(id, Assert.Single(await customers.SearchAsync(CustomerSearchField.Name, name[3..], cancellationToken: Ct)).Id);
        Assert.Contains(await customers.SearchAsync(CustomerSearchField.Egn, egn[..6], cancellationToken: Ct), c => c.Id == id);
        Assert.Equal(id, Assert.Single(await customers.SearchAsync(CustomerSearchField.Code, code.ToString(System.Globalization.CultureInfo.InvariantCulture), cancellationToken: Ct)).Id);
        Assert.Equal(id, Assert.Single(await customers.SearchAsync(CustomerSearchField.Phone, phone.Replace(" ", "", StringComparison.Ordinal)[2..], cancellationToken: Ct)).Id);

        Assert.Equal(CustomerErrors.EgnTaken, (await customers.CreateAsync(new CustomerDetails("Друг", egn, null, null), Ct)).Error);

        Assert.True((await customers.UpdateAsync(id, new CustomerDetails(name, egn, phone, null, IsActive: false), Ct)).IsSuccess);
        Assert.Empty(await customers.SearchAsync(CustomerSearchField.Egn, egn, cancellationToken: Ct));
        Assert.Single(await customers.SearchAsync(CustomerSearchField.Egn, egn, includeInactive: true, Ct));
    }

    [Fact]
    public async Task A_sale_keeps_its_customer_and_shows_in_the_customers_purchases_and_the_day_list()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, _) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (2, 400));
        Guid customer;
        await using (var scope = provider.CreateAsyncScope())
        {
            customer = (await scope.ServiceProvider.GetRequiredService<CustomersHandler>()
                .CreateAsync(new CustomerDetails(Apteka.IntegrationTests.Catalog.CatalogInputs.UniqueName("Купувач"), NewEgn(), null, null), Ct)).Value;
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var sold = await scope.ServiceProvider.GetRequiredService<CompleteSaleHandler>()
                .HandleAsync(new CheckoutInput([new CheckoutLineInput(product, box, 1)], PaymentMethod.Card, null, customer), Ct);
            Assert.True(sold.IsSuccess, sold.IsFailure ? sold.Error.Message : null);

            var view = (await scope.ServiceProvider.GetRequiredService<CustomersHandler>().GetAsync(customer, Ct))!;
            Assert.Equal(sold.Value.SaleId, Assert.Single(view.Sales).Id);

            var day = await scope.ServiceProvider.GetRequiredService<DailySalesHandler>().HandleAsync(BulgarianTime.Today(DateTimeOffset.UtcNow), Ct);
            Assert.Equal(view.Name, day.Sales.Single(s => s.Id == sold.Value.SaleId).CustomerName);

            var unknown = await scope.ServiceProvider.GetRequiredService<CompleteSaleHandler>()
                .HandleAsync(new CheckoutInput([new CheckoutLineInput(product, box, 1)], PaymentMethod.Card, null, Guid.CreateVersion7()), Ct);
            Assert.Equal(CustomerErrors.NotAvailable, unknown.Error);

            var context = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
            var delete = await Assert.ThrowsAsync<PostgresException>(() =>
                context.Database.ExecuteSqlAsync($"DELETE FROM sales.customers WHERE id = {customer}", Ct));
            Assert.Contains("never deleted", delete.MessageText, StringComparison.Ordinal);
        }
    }
}
