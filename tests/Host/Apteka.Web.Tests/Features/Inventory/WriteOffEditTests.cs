using Apteka.Catalog.Contracts;
using Apteka.Inventory.Application.Issuing;
using Apteka.Inventory.Application.Receiving;
using Apteka.Inventory.Application.WriteOffs;
using Apteka.Inventory.Contracts;
using Apteka.Inventory.Domain.Allocation;
using Apteka.Inventory.Domain.Batches;
using Apteka.Inventory.Domain.StockMovements;
using Apteka.Inventory.Domain.WriteOffs;
using Apteka.SharedKernel;
using Apteka.Web.Features.Inventory.WriteOffs;
using Apteka.Web.Tests.Features.Purchasing;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.Web.Tests.Features.Inventory;

public class WriteOffEditTests : BunitContext
{
    private readonly FakeCatalogLookup _catalog = new();
    private readonly FakeInventory _inventory = new();
    private readonly CatalogProductInfo _product;
    private readonly Batch _expired;
    private readonly Batch _good;

    public WriteOffEditTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var box = Guid.CreateVersion7();
        _product = new CatalogProductInfo(
            Guid.CreateVersion7(), "Аналгин 500 мг", 20m, CatalogDispensingMode.OverTheCounter, true, null,
            [new CatalogUnit(Guid.CreateVersion7(), "таблетка", 1), new CatalogUnit(box, "кутия", 20)], Code: 12);
        _catalog.Products.Add(_product);

        var today = DateOnly.FromDateTime(DateTime.Today);
        _expired = NewBatch("OLD", today.AddDays(-10));
        _good = NewBatch("NEW", today.AddDays(300));
        _inventory.Stock[_expired.Id] = 30;
        _inventory.Stock[_good.Id] = 40;

        Services.AddSingleton<ICatalogLookup>(_catalog);
        Services.AddSingleton<IStockQueries>(_inventory);
        Services.AddSingleton<IStockLocking>(_inventory);
        Services.AddSingleton<IInventoryRepository>(_inventory);
        Services.AddSingleton<IWriteOffRepository>(_inventory);
        Services.AddSingleton<IUnitOfWork>(new PassThroughUnitOfWork());
        Services.AddSingleton<ICurrentUser>(new FakeUser());
        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton<Apteka.Inventory.Application.Stocktakings.IStocktakingRepository>(new NoStocktakings());
        Services.AddScoped<CreateWriteOffHandler>();
    }

    private Batch NewBatch(string number, DateOnly expiry)
    {
        var batch = Batch.Create(new BatchSpec(_product.ProductId, number, expiry, StockType.Retail, 0.25m, 20, 8m, "Delivery", Guid.CreateVersion7(), Guid.CreateVersion7()), DateTimeOffset.UtcNow).Value;
        _inventory.Batches.Add(batch);
        return batch;
    }

    [Fact]
    public void Expired_batch_is_offered_first_and_Ctrl_Enter_twice_writes_it_off()
    {
        var page = Render<WriteOffEdit>();
        page.Find("#reason").Change("изтекъл срок");
        var search = page.Find("#product-search");
        search.Input("12");
        search.KeyDown("Enter");

        Assert.Contains("(изтекла)", page.Find(".batches tr.selected").TextContent);
        page.Find("#quantity").Input("1");
        page.Find("#quantity").KeyDown("Enter");
        Assert.Contains("OLD", page.Find("table:not(.batches) tbody tr").TextContent);

        page.Find("div").KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });
        Assert.Contains("Потвърди приключването", page.Markup);
        Assert.Empty(_inventory.WriteOffs);
        page.Find("div").KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        var writeOff = Assert.Single(_inventory.WriteOffs);
        Assert.Equal("изтекъл срок", writeOff.Reason);
        Assert.Equal(_expired.Id, Assert.Single(writeOff.Lines).BatchId);
        Assert.Equal(-20, Assert.Single(_inventory.Movements).QuantityDelta);
        Assert.EndsWith($"inventory/write-offs/{writeOff.Id}", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void More_than_the_batch_has_is_not_added()
    {
        var page = Render<WriteOffEdit>();
        var search = page.Find("#product-search");
        search.Input("12");
        search.KeyDown("Enter");

        page.Find("#quantity").Input("2"); // 2 boxes = 40 tablets, the expired batch has 30
        page.Find("#quantity").KeyDown("Enter");

        Assert.Contains("има само 1 × кутия + 10 × таблетка", page.Find(".alert.error").TextContent);
    }

    private sealed class FakeInventory : IStockQueries, IStockLocking, IInventoryRepository, IWriteOffRepository
    {
        public List<Batch> Batches { get; } = [];

        public Dictionary<Guid, int> Stock { get; } = [];

        public List<StockMovement> Movements { get; } = [];

        public List<WriteOff> WriteOffs { get; } = [];

        public Task<IReadOnlyList<BatchStock>> GetBatchesAsync(Guid productId, DateOnly? expiringOnOrBefore, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BatchStock>>(Batches
                .Where(b => b.ProductId == productId)
                .OrderBy(b => b.ExpiryDate)
                .Select(b => new BatchStock(b.Id, b.BatchNo, b.ExpiryDate, Stock[b.Id], b.PurchasePrice, b.SalePrice, b.PricedUnitFactor, b.PricedUnitSalePrice, b.CreatedAt))
                .ToList());

        public Task<IReadOnlyList<BatchAvailability>> LockRetailBatchesAsync(Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BatchAvailability>>(Batches
                .Where(b => b.ProductId == productId)
                .Select(b => new BatchAvailability(b.Id, b.ExpiryDate, b.CreatedAt, Stock[b.Id]))
                .ToList());

        public Task<IReadOnlyDictionary<Guid, Batch>> GetBatchesAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Batch>>(Batches.Where(b => batchIds.Contains(b.Id)).ToDictionary(b => b.Id));

        public void Add(StockMovement movement) => Movements.Add(movement);

        public void Add(WriteOff writeOff) => WriteOffs.Add(writeOff);

        public void Add(Batch batch) => Batches.Add(batch);

        public Task<IReadOnlyList<Batch>> GetBySourceLinesAsync(IReadOnlyCollection<Guid> sourceLineIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Batch>>(Batches.Where(b => sourceLineIds.Contains(b.SourceLineId)).ToList());

        public Task<bool> AnyReceivedAsync(IReadOnlyCollection<Guid> sourceLineIds, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<Result> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success());

        public Task<WriteOff?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(WriteOffs.SingleOrDefault(w => w.Id == id));

        public Task<IReadOnlyList<WriteOffListItem>> ListAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<WriteOffListItem>>([]);

        public Task<IReadOnlyList<ProductStock>> GetProductStockAsync(IReadOnlyCollection<Guid>? productIds, DateOnly? expiringOnOrBefore, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductStock>>([]);

        public Task<IReadOnlyList<ProductBatchStock>> GetAllBatchesAsync(IReadOnlyCollection<Guid>? productIds, DateOnly? expiringOnOrBefore, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductBatchStock>>([]);

        public Task<ProductCard> GetProductCardAsync(Guid productId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProductCard(0, []));

        public Task<IReadOnlyDictionary<Guid, int>> GetSoldQuantitiesAsync(IReadOnlyCollection<Guid>? productIds, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>());

        public Task<IReadOnlyDictionary<Guid, decimal>> GetPurchasePricesAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(new Dictionary<Guid, decimal>());

        public Task<LastReceipt?> GetLastReceiptAsync(Guid productId, CancellationToken cancellationToken = default) => Task.FromResult<LastReceipt?>(null);
    }

    private sealed class NoStocktakings : Apteka.Inventory.Application.Stocktakings.IStocktakingRepository
    {
        public void Add(Apteka.Inventory.Domain.Stocktakings.Stocktaking stocktaking)
        {
        }

        public Task<Apteka.Inventory.Domain.Stocktakings.Stocktaking?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Apteka.Inventory.Domain.Stocktakings.Stocktaking?>(null);

        public Task<bool> AnyOpenAsync(CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<long?> FindOpenForProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken) => Task.FromResult<long?>(null);

        public Task<long?> FindOpenForBatchesAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken) => Task.FromResult<long?>(null);

        public Task<IReadOnlyList<Apteka.Inventory.Application.Stocktakings.StocktakingListItem>> ListAsync(int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Apteka.Inventory.Application.Stocktakings.StocktakingListItem>>([]);

        public Task<Result> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success());
    }
}
