using Apteka.Catalog.Domain.Products;
using Apteka.Inventory.Application.BatchBlocks;
using Apteka.Inventory.Application.WriteOffs;
using Apteka.Inventory.Contracts;
using Apteka.IntegrationTests.Infrastructure;
using Apteka.IntegrationTests.Purchasing;
using Apteka.Sales.Application.BatchBuyers;
using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Application.Customers;
using Apteka.Sales.Domain.Customers;
using Apteka.Sales.Domain.Sales;
using Microsoft.Extensions.DependencyInjection;
using static Apteka.IntegrationTests.Purchasing.DeliveryFixtures;

namespace Apteka.IntegrationTests.Inventory;

[Collection(PostgresCollection.Name)]
public class BatchRecallTests(PostgresFixture postgres)
{
    [Fact]
    public async Task A_blocked_batch_is_not_sold_but_can_be_written_off_and_its_buyers_are_listed()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, _) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (2, 100), (1, 400));
        await using var scope = provider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var stock = services.GetRequiredService<IStockQueries>();
        var early = (await stock.GetBatchesAsync(product, null, Ct)).Single(b => b.BatchNo == "B1");

        // A customer buys a box from the early batch before the recall.
        var customer = (await services.GetRequiredService<CustomersHandler>()
            .CreateAsync(new CustomerDetails(Apteka.IntegrationTests.Catalog.CatalogInputs.UniqueName("Изтегляне"), null, "0888 111 222", null), Ct)).Value;
        Assert.True((await services.GetRequiredService<CompleteSaleHandler>()
            .HandleAsync(new CheckoutInput([new CheckoutLineInput(product, box, 1)], PaymentMethod.Card, null, customer), Ct)).IsSuccess);

        Assert.True((await services.GetRequiredService<BlockBatchHandler>().HandleAsync(early.BatchId, true, "ИАЛ писмо 1/2026", Ct)).IsSuccess);
        var info = (await stock.GetBatchAsync(early.BatchId, Ct))!;
        Assert.True(info.IsBlocked);
        Assert.Equal("ИАЛ писмо 1/2026", Assert.Single(info.BlockHistory).Reason);

        // The next sale takes the later batch; FEFO skips the blocked one.
        var sold = await services.GetRequiredService<CompleteSaleHandler>()
            .HandleAsync(new CheckoutInput([new CheckoutLineInput(product, box, 1)], PaymentMethod.Card, null), Ct);
        Assert.True(sold.IsSuccess);
        Assert.Equal(20, (await stock.GetBatchAsync(early.BatchId, Ct))!.BaseQuantity);
        var refused = await services.GetRequiredService<CompleteSaleHandler>()
            .HandleAsync(new CheckoutInput([new CheckoutLineInput(product, box, 1)], PaymentMethod.Card, null), Ct);
        Assert.Equal("sales.not_enough_stock", refused.Error.Code);

        Assert.True((await services.GetRequiredService<CreateWriteOffHandler>()
            .HandleAsync(new WriteOffInput("изтегляне", [new WriteOffLineInput(product, box, 1, early.BatchId)]), Ct)).IsSuccess);

        var buyer = Assert.Single(await services.GetRequiredService<BatchBuyersHandler>().HandleAsync(early.BatchId, Ct));
        Assert.Equal((customer, "0888 111 222", 20), (buyer.CustomerId!.Value, buyer.CustomerPhone, buyer.KeptBaseQuantity));

        Assert.True((await services.GetRequiredService<BlockBatchHandler>().HandleAsync(early.BatchId, false, null, Ct)).IsSuccess);
        Assert.False((await stock.GetBatchAsync(early.BatchId, Ct))!.IsBlocked);
    }
}
