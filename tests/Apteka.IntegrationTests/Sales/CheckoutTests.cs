using Apteka.Audit.Infrastructure;
using Apteka.Catalog.Domain.Products;
using Apteka.Inventory.Contracts;
using Apteka.Inventory.Infrastructure;
using Apteka.IntegrationTests.Infrastructure;
using Apteka.IntegrationTests.Purchasing;
using Apteka.Sales.Application.Checkout;
using Apteka.Sales.Application.DailySales;
using Apteka.Sales.Application.Reversals;
using Apteka.Sales.Domain.Sales;
using Apteka.Sales.Infrastructure;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Apteka.IntegrationTests.Purchasing.DeliveryFixtures;

namespace Apteka.IntegrationTests.Sales;

[Collection(PostgresCollection.Name)]
public class CheckoutTests(PostgresFixture postgres)
{
    private static async Task<Result<CompletedSale>> SellAsync(IServiceProvider provider, params CheckoutLineInput[] lines) =>
        await SellAsync(provider, PaymentMethod.Card, null, lines);

    private static async Task<Result<CompletedSale>> SellAsync(IServiceProvider provider, PaymentMethod payment, decimal? cash, params CheckoutLineInput[] lines)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CompleteSaleHandler>().HandleAsync(new CheckoutInput(lines, payment, cash), Ct);
    }

    [Fact]
    public async Task A_sale_reduces_the_stock_and_takes_the_price_from_the_batch()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, tablet) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (3, 400));

        var sold = await SellAsync(provider, PaymentMethod.Cash, 20m, new CheckoutLineInput(product, box, 2), new CheckoutLineInput(product, tablet, 5));

        Assert.True(sold.IsSuccess, sold.IsFailure ? sold.Error.Message : null);
        Assert.Equal((2 * 6.00m) + (5 * 0.30m), sold.Value.Total);
        Assert.Equal(20m - 13.50m, sold.Value.Change);
        Assert.True(sold.Value.Number > 0);
        Assert.Equal(60 - 45, await StockFixtures.StockAsync(provider, product));
    }

    [Fact]
    public async Task A_sale_over_the_stock_is_refused_and_nothing_changes()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, _) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (1, 400));
        var (other, otherBox, _) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (5, 400));

        var sold = await SellAsync(provider, new CheckoutLineInput(other, otherBox, 1), new CheckoutLineInput(product, box, 2));

        Assert.Equal("sales.not_enough_stock", sold.Error.Code);
        Assert.Contains("20", sold.Error.Message);
        Assert.Equal(20, await StockFixtures.StockAsync(provider, product));
        Assert.Equal(100, await StockFixtures.StockAsync(provider, other));
    }

    [Fact]
    public async Task Two_tills_selling_the_last_box_at_the_same_time_only_one_sale_goes_through()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, _) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (1, 400));

        var results = await Task.WhenAll(
            Task.Run(() => SellAsync(provider, new CheckoutLineInput(product, box, 1))),
            Task.Run(() => SellAsync(provider, new CheckoutLineInput(product, box, 1))));

        Assert.Single(results, r => r.IsSuccess);
        Assert.Single(results, r => r.IsFailure && r.Error.Code == "sales.not_enough_stock");
        Assert.Equal(0, await StockFixtures.StockAsync(provider, product));
    }

    [Fact]
    public async Task Stock_is_issued_first_from_the_nearest_expiry_and_split_over_batches()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, _) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (5, 400), (1, 60));

        var sold = await SellAsync(provider, new CheckoutLineInput(product, box, 2));
        Assert.True(sold.IsSuccess, sold.IsFailure ? sold.Error.Message : null);

        await using var scope = provider.CreateAsyncScope();
        var batches = await scope.ServiceProvider.GetRequiredService<IStockQueries>().GetBatchesAsync(product, null, Ct);
        // B2 (expires first, 1 box) is used up; the second box came from B1 (100 → 80).
        Assert.Equal([("B1", 80)], batches.Select(b => (b.BatchNo, b.BaseQuantity)).ToList());

        var sale = await scope.ServiceProvider.GetRequiredService<SalesDbContext>().Sales
            .Include(s => s.Lines).ThenInclude(l => l.Batches).SingleAsync(s => s.Id == sold.Value.SaleId, Ct);
        Assert.Equal([20, 20], sale.Lines.Single().Batches.Select(b => b.BaseQuantity));
    }

    [Fact]
    public async Task Expired_batches_are_not_sold()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, _) = await DeliveryFixtures.ProductAsync(provider);

        // An expired batch can only come from an old delivery; put it in stock directly.
        await using (var scope = provider.CreateAsyncScope())
        {
            var received = await scope.ServiceProvider.GetRequiredService<IStockReceiving>().ReceiveAsync(
                new StockReceipt("Test", Guid.CreateVersion7(), null,
                    [new StockReceiptLine(Guid.CreateVersion7(), product, "OLD", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10), 40, 0.2m, 20, 6m)]),
                Ct);
            Assert.True(received.IsSuccess);
        }

        var sold = await SellAsync(provider, new CheckoutLineInput(product, box, 1));

        Assert.Equal("sales.not_enough_stock", sold.Error.Code);
        Assert.Equal(40, await StockFixtures.StockAsync(provider, product));
    }

    [Fact]
    public async Task Narcotics_are_refused_and_prescription_products_need_a_confirmed_prescription()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var narcotic = await StockFixtures.StockedProductAsync(provider, DispensingMode.Narcotic, (2, 400));
        var rx = await StockFixtures.StockedProductAsync(provider, DispensingMode.Prescription, (2, 400));

        Assert.Equal("sales.narcotic_not_allowed", (await SellAsync(provider, new CheckoutLineInput(narcotic.ProductId, narcotic.BoxId, 1))).Error.Code);
        Assert.Equal("sales.prescription_not_confirmed", (await SellAsync(provider, new CheckoutLineInput(rx.ProductId, rx.BoxId, 1))).Error.Code);
        Assert.True((await SellAsync(provider, new CheckoutLineInput(rx.ProductId, rx.BoxId, 1, PrescriptionConfirmed: true))).IsSuccess);
        Assert.Equal(40, await StockFixtures.StockAsync(provider, narcotic.ProductId));
    }

    [Fact]
    public async Task Reversal_returns_the_stock_to_the_same_batches_once_and_keeps_the_sale()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, _) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (2, 400));
        var sold = (await SellAsync(provider, new CheckoutLineInput(product, box, 2))).Value;
        Assert.Equal(0, await StockFixtures.StockAsync(provider, product));

        await using (var scope = provider.CreateAsyncScope())
        {
            var reverse = scope.ServiceProvider.GetRequiredService<ReverseSaleHandler>();
            Assert.True((await reverse.HandleAsync(sold.SaleId, "грешка", Ct)).IsSuccess);
            Assert.Equal(SalesErrors.AlreadyReversed, (await reverse.HandleAsync(sold.SaleId, null, Ct)).Error);
        }

        Assert.Equal(40, await StockFixtures.StockAsync(provider, product));
        await using var check = provider.CreateAsyncScope();
        var sale = (await check.ServiceProvider.GetRequiredService<GetSaleHandler>().HandleAsync(sold.SaleId, Ct))!;
        Assert.Equal(SaleStatus.Reversed, sale.Status);
        Assert.Equal(12.00m, sale.Total);

        var movements = await check.ServiceProvider.GetRequiredService<InventoryDbContext>().StockMovements
            .Where(m => m.DocumentId == sold.SaleId || m.DocumentType == ReverseSaleHandler.DocumentType)
            .Select(m => new { m.DocumentId, m.QuantityDelta, m.Kind })
            .ToListAsync(Ct);
        Assert.Contains(movements, m => m.DocumentId == sold.SaleId && m.QuantityDelta == -40);
    }

    [Fact]
    public async Task A_sale_writes_audit_rows_and_appears_in_the_day_list_with_payment_totals()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, box, _) = await StockFixtures.StockedProductAsync(provider, DispensingMode.OverTheCounter, (5, 400));
        var cash = (await SellAsync(provider, PaymentMethod.Cash, 10m, new CheckoutLineInput(product, box, 1))).Value;
        var card = (await SellAsync(provider, new CheckoutLineInput(product, box, 2, DiscountPercent: 10m))).Value;

        await using var scope = provider.CreateAsyncScope();
        var audit = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        Assert.True(await audit.AuditLog.AnyAsync(r => r.Entity == nameof(Sale) && r.Action == "Insert" && r.EntityId == cash.SaleId.ToString(), Ct));
        Assert.True(await audit.AuditLog.AnyAsync(r => r.Entity == "StockMovement" && r.Action == "Insert", Ct));

        Assert.Equal(10.80m, card.Total);
        var day = await scope.ServiceProvider.GetRequiredService<DailySalesHandler>()
            .HandleAsync(BulgarianTime.Today(DateTimeOffset.UtcNow), Ct);
        Assert.Contains(day.Sales, s => s.Id == cash.SaleId && s.PaymentMethod == PaymentMethod.Cash);
        Assert.Contains(day.Sales, s => s.Id == card.SaleId && s.PaymentMethod == PaymentMethod.Card);
        Assert.True(day.CashTotal >= 6.00m && day.CardTotal >= 10.80m);
    }
}
