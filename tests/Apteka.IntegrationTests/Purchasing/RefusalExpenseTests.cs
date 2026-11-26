using Apteka.Catalog.Domain.Products;
using Apteka.IntegrationTests.Infrastructure;
using Apteka.Purchasing.Application.Expenses;
using Apteka.Purchasing.Application.Orders;
using Apteka.Purchasing.Application.Refusals;
using Apteka.Purchasing.Domain.Expenses;
using Apteka.Purchasing.Domain.Refusals;
using Apteka.Purchasing.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using static Apteka.IntegrationTests.Purchasing.DeliveryFixtures;

namespace Apteka.IntegrationTests.Purchasing;

[Collection(PostgresCollection.Name)]
public class RefusalExpenseTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Refusals_are_demand_in_the_order_proposal_and_free_text_is_kept()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        var (product, _, _) = await ProductAsync(provider); // no stock at all, boxes of 20

        await using var scope = provider.CreateAsyncScope();
        var refusals = scope.ServiceProvider.GetRequiredService<RefusalsHandler>();
        Assert.True((await refusals.AddAsync(product, null, 30, "ще мине утре", Ct)).IsSuccess);
        Assert.True((await refusals.AddAsync(null, "Непозната мехлем", 1, null, Ct)).IsSuccess);
        Assert.Equal(RefusalErrors.WhatRequired, (await refusals.AddAsync(null, " ", 1, null, Ct)).Error);

        var listed = await refusals.ListAsync(Today.AddDays(-2), Today.AddDays(1), Ct);
        Assert.Contains(listed, r => r.Text == "Непозната мехлем");

        var proposal = (await scope.ServiceProvider.GetRequiredService<SuggestOrderHandler>().HandleAsync(OrderCriterion.SalesLessStock, 7, null, Ct)).Value;
        var line = Assert.Single(proposal, p => p.ProductId == product);
        Assert.Equal((30, 2), (line.Sold, line.Quantity)); // 30 tablets refused → 2 boxes of 20
    }

    [Fact]
    public async Task Expenses_are_listed_by_date_and_never_change()
    {
        await using var provider = await ModuleServices.CreateAsync(postgres.ConnectionString);
        await using var scope = provider.CreateAsyncScope();
        var expenses = scope.ServiceProvider.GetRequiredService<ExpensesHandler>();
        var day = new DateOnly(2026, 3, 15);

        var rent = (await expenses.AddAsync(day, "Наем", 500.004m, "Ф-1", null, Ct)).Value;
        Assert.True((await expenses.AddAsync(day, "Наем", -50m, null, "грешка в сумата", Ct)).IsSuccess);
        Assert.Equal(ExpenseErrors.AmountRequired, (await expenses.AddAsync(day, "Наем", 0m, null, null, Ct)).Error);

        var march = await expenses.ListAsync(day, day, Ct);
        Assert.Equal(450.00m, march.Sum(e => e.Amount));

        var context = scope.ServiceProvider.GetRequiredService<PurchasingDbContext>();
        var delete = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync($"DELETE FROM purchasing.expenses WHERE id = {rent}", Ct));
        Assert.Contains("append-only", delete.MessageText, StringComparison.Ordinal);
    }
}
