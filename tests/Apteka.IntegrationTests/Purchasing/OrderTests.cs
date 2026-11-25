using Apteka.Catalog.Domain.Products;
using Apteka.IntegrationTests.Infrastructure;
using Apteka.Purchasing.Application.Orders;
using Apteka.Purchasing.Domain.Orders;
using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Domain.Sales;
using Microsoft.Extensions.DependencyInjection;
using static Apteka.IntegrationTests.Purchasing.DeliveryFixtures;

namespace Apteka.IntegrationTests.Purchasing;

[Collection(PostgresCollection.Name)]
public class OrderTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Proposal_by_sales_less_stock_and_by_minimum_rounds_up_to_boxes_and_the_order_is_saved_and_sent()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, tablet) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (3, 400)); // 60 tablets, boxes of 20
        Guid group;

        await using (var scope = provider.CreateAsyncScope())
        {
            // Put the product in a group of its own with a minimum of 50 and a maximum of 100 tablets.
            var services = scope.ServiceProvider;
            group = (await services.GetRequiredService<Apteka.Catalog.Application.ProductGroups.ProductGroupsHandler>()
                .CreateAsync(Apteka.IntegrationTests.Catalog.CatalogInputs.UniqueName("Заявка"), Ct)).Value;
            var view = (await services.GetRequiredService<Apteka.Catalog.Application.Products.GetProductHandler>().HandleAsync(product, Ct))!;
            var input = new Apteka.Catalog.Application.Products.ProductInput(
                view.Name, view.AtcCode, view.DispensingMode, view.VatRate, view.IsActive,
                view.Units.Select(u => new Apteka.Catalog.Application.Products.UnitInput(u.Id, u.Name, u.Factor)).ToList(),
                view.Barcodes.Select(b => new Apteka.Catalog.Application.Products.BarcodeInput(b.Id, b.Code, b.UnitName)).ToList(),
                ProductGroupId: group, MinStock: 50, MaxStock: 100);
            Assert.True((await services.GetRequiredService<Apteka.Catalog.Application.Products.UpdateProductHandler>().HandleAsync(product, input, Ct)).IsSuccess);

            // Sell 2 boxes and 5 tablets: 45 sold, 15 left.
            var sold = await services.GetRequiredService<CompleteSaleHandler>().HandleAsync(
                new CheckoutInput([new CheckoutLineInput(product, box, 2), new CheckoutLineInput(product, tablet, 5)], PaymentMethod.Card, null), Ct);
            Assert.True(sold.IsSuccess, sold.IsFailure ? sold.Error.Message : null);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var suggest = scope.ServiceProvider.GetRequiredService<SuggestOrderHandler>();

            var bySales = Assert.Single((await suggest.HandleAsync(OrderCriterion.SalesLessStock, 7, group, Ct)).Value);
            Assert.Equal((45, 15, "кутия", 2), (bySales.Sold, bySales.OnHand, bySales.UnitName, bySales.Quantity)); // 45 − 15 = 30 → 2 boxes

            var byMinimum = Assert.Single((await suggest.HandleAsync(OrderCriterion.MinStock, 0, group, Ct)).Value);
            Assert.Equal(5, byMinimum.Quantity); // 100 − 15 = 85 → 5 boxes

            Assert.Equal(OrderErrors.InvalidDays, (await suggest.HandleAsync(OrderCriterion.Sales, 0, group, Ct)).Error);

            var orders = scope.ServiceProvider.GetRequiredService<OrdersHandler>();
            var supplier = await SupplierAsync(provider);
            var saved = await orders.SaveAsync(null, new OrderInput(supplier, "по продажби", [new OrderLineInput(product, bySales.UnitId, bySales.Quantity)]), Ct);
            Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Error.Message : null);
            Assert.True((await orders.MarkSentAsync(saved.Value, Ct)).IsSuccess);

            var view = (await orders.GetAsync(saved.Value, Ct))!;
            Assert.True(view.Number > 0);
            Assert.Equal(PurchaseOrderStatus.Sent, view.Status);
            Assert.Equal(("кутия", 2), (view.Lines.Single().UnitName, view.Lines.Single().Quantity));
            Assert.Contains(await orders.ListAsync(Ct), o => o.Id == saved.Value && o.LineCount == 1);
        }
    }
}
