using Apteka.Catalog.Contracts;
using Apteka.Inventory.Application.Issuing;
using Apteka.Inventory.Application.Receiving;
using Apteka.Inventory.Application.Stocktakings;
using Apteka.Inventory.Domain.Allocation;
using Apteka.Inventory.Domain.Batches;
using Apteka.Inventory.Domain.StockMovements;
using Apteka.Inventory.Domain.Stocktakings;
using Apteka.SharedKernel;
using Apteka.Web.Features.Inventory.Stocktakings;
using Apteka.Web.Tests.Features.Purchasing;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Web.Tests.Features.Inventory;

public class StocktakingProtocolTests : BunitContext
{
    private readonly FakeCatalogLookup _catalog = new();
    private readonly FakeStocktakings _stocktakings = new();
    private readonly Stocktaking _stocktaking;
    private readonly Guid _batch = Guid.CreateVersion7();

    public StocktakingProtocolTests()
    {
        var product = new CatalogProductInfo(
            Guid.CreateVersion7(), "Аналгин 500 мг", 20m, CatalogDispensingMode.OverTheCounter, true, null,
            [new CatalogUnit(Guid.CreateVersion7(), "таблетка", 1), new CatalogUnit(Guid.CreateVersion7(), "кутия", 20)]);
        _catalog.Products.Add(product);
        _stocktaking = Stocktaking.Start(
            Guid.CreateVersion7(), "Всички стоки",
            [new StocktakingLineSpec(product.ProductId, product.Name, _batch, "L1", new DateOnly(2027, 5, 1), 45, 0.2m)],
            null, DateTimeOffset.UtcNow).Value;
        _stocktakings.Item = _stocktaking;

        Services.AddSingleton<ICatalogLookup>(_catalog);
        Services.AddSingleton<IStocktakingRepository>(_stocktakings);
        Services.AddSingleton<IStockLocking>(_stocktakings);
        Services.AddSingleton<IInventoryRepository>(_stocktakings);
        Services.AddSingleton<IUnitOfWork>(new PassThroughUnitOfWork());
        Services.AddSingleton<ICurrentUser>(new FakeUser());
        Services.AddSingleton(TimeProvider.System);
        Services.AddScoped<GetStocktakingHandler>();
        Services.AddScoped<CountStocktakingHandler>();
        Services.AddScoped<CloseStocktakingHandler>();
    }

    [Fact]
    public void Boxes_and_pieces_are_counted_in_base_units_and_Ctrl_Enter_twice_closes_with_the_difference()
    {
        var page = Render<StocktakingProtocol>(p => p.Add(x => x.Id, _stocktaking.Id));
        Assert.Contains("2 × кутия + 5 × таблетка (45)", page.Markup);

        page.Find("input.counted.big").Change("2");
        page.Find("input.counted:not(.big)").Change("3");
        Assert.Equal(43, _stocktaking.Lines[0].CountedQuantity);
        Assert.Contains("−2", page.Find("tbody tr").TextContent);

        var keys = page.FindAll("div").Last(d => d.QuerySelector("input[type=search]") is not null && d.QuerySelector("table") is not null);
        keys.KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });
        Assert.Equal(StocktakingStatus.Open, _stocktaking.Status);
        page.FindAll("div").Last(d => d.QuerySelector("input[type=search]") is not null && d.QuerySelector("table") is not null)
            .KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        Assert.Equal(StocktakingStatus.Closed, _stocktaking.Status);
        Assert.Equal(-2, Assert.Single(_stocktakings.Movements).QuantityDelta);
        Assert.Contains("приключена", page.Markup);
    }

    private sealed class FakeStocktakings : IStocktakingRepository, IStockLocking, IInventoryRepository
    {
        public Stocktaking? Item { get; set; }

        public List<StockMovement> Movements { get; } = [];

        public void Add(Stocktaking stocktaking) => Item = stocktaking;

        public Task<Stocktaking?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Item?.Id == id ? Item : null);

        public Task<bool> AnyOpenAsync(CancellationToken cancellationToken) => Task.FromResult(Item?.Status == StocktakingStatus.Open);

        public Task<long?> FindOpenForProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken) => Task.FromResult<long?>(null);

        public Task<long?> FindOpenForBatchesAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken) => Task.FromResult<long?>(null);

        public Task<IReadOnlyList<StocktakingListItem>> ListAsync(int limit, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<StocktakingListItem>>([]);

        public Task<Result> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success());

        public Task<IReadOnlyList<BatchAvailability>> LockRetailBatchesAsync(Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BatchAvailability>>(Item!.Lines.Where(l => l.ProductId == productId)
                .Select(l => new BatchAvailability(l.BatchId, l.ExpiryDate, DateTimeOffset.UtcNow, 45)).ToList());

        public Task<IReadOnlyDictionary<Guid, Batch>> GetBatchesAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Batch>>(new Dictionary<Guid, Batch>());

        public Task<IReadOnlyList<Batch>> GetBySourceLinesAsync(IReadOnlyCollection<Guid> sourceLineIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Batch>>([]);

        public Task<bool> AnyReceivedAsync(IReadOnlyCollection<Guid> sourceLineIds, CancellationToken cancellationToken) => Task.FromResult(false);

        public void Add(Batch batch)
        {
        }

        public void Add(StockMovement movement) => Movements.Add(movement);
    }
}
