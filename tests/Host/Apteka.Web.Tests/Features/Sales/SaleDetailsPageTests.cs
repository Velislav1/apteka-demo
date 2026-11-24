using Apteka.Identity.Contracts;
using Apteka.Inventory.Contracts;
using Apteka.Sales.Application.Common;
using Apteka.Sales.Application.Customers;
using Apteka.Sales.Application.DailySales;
using Apteka.Sales.Application.Returns;
using Apteka.Sales.Application.Reversals;
using Apteka.Sales.Domain.Sales;
using Apteka.SharedKernel;
using Apteka.Web.Features.Sales.SaleDetails;
using Apteka.Web.Tests.Features.Purchasing;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Web.Tests.Features.Sales;

public class SaleDetailsPageTests : BunitContext
{
    private readonly FakeSales _sales = new();
    private readonly ReturnsToStock _stock = new();
    private readonly Sale _sale;

    public SaleDetailsPageTests()
    {
        _sale = Sale.Create(
            Guid.CreateVersion7(),
            [new SaleLineSpec(Guid.CreateVersion7(), Guid.CreateVersion7(), "Аналгин", Guid.CreateVersion7(), "кутия", 20, 3, 6.00m, 0m, 20m,
                [new SaleLineBatchSpec(Guid.CreateVersion7(), 60)])],
            PaymentMethod.Cash, 20m, null, DateTimeOffset.UtcNow).Value;
        _sales.Add(_sale);

        Services.AddSingleton<ISaleRepository>(_sales);
        Services.AddSingleton<ICustomerRepository>(new FakeCustomers());
        Services.AddSingleton<IStockIssuing>(_stock);
        Services.AddSingleton<IUserDirectory>(new NoUsers());
        Services.AddSingleton<IUnitOfWork>(new PassThroughUnitOfWork());
        Services.AddSingleton(TimeProvider.System);
        Services.AddScoped<GetSaleHandler>();
        Services.AddScoped<CreateReturnHandler>();
        Services.AddScoped<ReverseSaleHandler>();
    }

    private IRenderedComponent<SaleDetailsPage> Open(string role)
    {
        Services.AddSingleton<ICurrentUser>(new FakeUser(role));
        var auth = AddAuthorization();
        auth.SetAuthorized("test");
        auth.SetRoles(role);
        return Render<SaleDetailsPage>(p => p.Add(x => x.Id, _sale.Id));
    }

    [Fact]
    public void Manager_gives_back_one_box_with_Ctrl_Enter_twice_and_is_told_the_cash_refund()
    {
        var page = Open(Roles.Manager);

        page.Find("input.back").Input("1");
        var keys = page.FindAll("div").First(d => d.QuerySelector("input.back") is not null && d.QuerySelector("table") is not null);
        keys.KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });
        Assert.Empty(_sales.Returns);
        page.FindAll("div").First(d => d.QuerySelector("input.back") is not null && d.QuerySelector("table") is not null)
            .KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        Assert.Equal(6.00m, Assert.Single(_sales.Returns).Total);
        Assert.Equal(20, Assert.Single(Assert.Single(_stock.Returns).Batches).BaseQuantity);
        Assert.Contains("6,00 € в брой", page.Find(".alert.success").TextContent);
        Assert.Contains("с корекция", page.Find("h1").TextContent);
        Assert.DoesNotContain("Сторно на цялата продажба", page.Markup);
    }

    [Fact]
    public void Pharmacist_sees_the_sale_but_cannot_correct_it()
    {
        var page = Open(Roles.Pharmacist);

        Assert.Contains("Аналгин", page.Markup);
        Assert.Empty(page.FindAll("input.back"));
    }

    private sealed class ReturnsToStock : IStockIssuing
    {
        public List<StockReturn> Returns { get; } = [];

        public Task<Result<IReadOnlyList<IssuedLine>>> IssueAsync(StockIssue issue, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result> ReturnAsync(StockReturn stockReturn, CancellationToken cancellationToken = default)
        {
            Returns.Add(stockReturn);
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class NoUsers : IUserDirectory
    {
        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<IReadOnlyList<LoginUser>> ListActiveAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LoginUser>>([]);
    }
}
