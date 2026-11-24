using Apteka.Catalog.Contracts;
using Apteka.Inventory.Contracts;
using Apteka.Purchasing.Application.Deliveries;
using Apteka.Purchasing.Application.Suppliers;
using Apteka.Purchasing.Domain.Deliveries;
using Apteka.Purchasing.Domain.Suppliers;
using Apteka.SharedKernel;

namespace Apteka.Web.Tests.Features.Purchasing;

internal sealed class FakeCatalogLookup : ICatalogLookup
{
    public List<CatalogProductInfo> Products { get; } = [];

    public Dictionary<string, (Guid ProductId, Guid UnitId)> Barcodes { get; } = [];

    public Task<CatalogUnitInfo?> FindByBarcodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (!Barcodes.TryGetValue(code, out var hit))
        {
            return Task.FromResult<CatalogUnitInfo?>(null);
        }

        var product = Products.Single(p => p.ProductId == hit.ProductId);
        var unit = product.Units.Single(u => u.UnitId == hit.UnitId);
        return Task.FromResult<CatalogUnitInfo?>(new CatalogUnitInfo(
            product.ProductId, product.Name, product.VatRate, product.DispensingMode, product.IsActive, unit.UnitId, unit.Name, unit.Factor));
    }

    public Task<CatalogProductInfo?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Products.SingleOrDefault(p => p.ProductId == productId));

    public Task<IReadOnlyList<CatalogProductInfo>> GetProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CatalogProductInfo>>(Products.Where(p => productIds.Contains(p.ProductId)).ToList());

    public Task<IReadOnlyList<CatalogProductInfo>> SearchProductsAsync(string text, int limit, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CatalogProductInfo>>(
            Products.Where(p => p.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase)).Take(limit).ToList());
}

internal sealed class FakeSupplierRepository : ISupplierRepository
{
    public List<Supplier> Items { get; } = [];

    public Task<IReadOnlyList<SupplierView>> ListAsync(bool includeInactive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SupplierView>>(Items.Select(s => new SupplierView(s.Id, s.Name, s.Eik, s.PaymentTermDays, s.IsActive)).ToList());

    public Task<Supplier?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Items.SingleOrDefault(s => s.Id == id));

    public Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken) => Task.FromResult(false);

    public Task<bool> EikExistsAsync(string eik, Guid? exceptId, CancellationToken cancellationToken) => Task.FromResult(false);

    public void Add(Supplier supplier) => Items.Add(supplier);

    public Task<Result> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success());
}

internal sealed class FakeDeliveryRepository : IDeliveryRepository
{
    public List<Delivery> Items { get; } = [];

    public Task<Delivery?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Items.SingleOrDefault(d => d.Id == id));

    public Task<bool> InvoiceExistsAsync(Guid supplierId, string invoiceNo, Guid exceptId, CancellationToken cancellationToken) => Task.FromResult(false);

    public Task<IReadOnlyList<DeliveryListItem>> ListAsync(Guid? supplierId, DeliveryStatus? status, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DeliveryListItem>>([]);

    public void Add(Delivery delivery) => Items.Add(delivery);

    public Task<Result> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success());
}

internal sealed class FakeStockReceiving : IStockReceiving
{
    public List<StockReceipt> Receipts { get; } = [];

    public Task<Result> ReceiveAsync(StockReceipt receipt, CancellationToken cancellationToken = default)
    {
        Receipts.Add(receipt);
        return Task.FromResult(Result.Success());
    }
}

internal sealed class PassThroughUnitOfWork : IUnitOfWork
{
    public Task<Result> ExecuteAsync(Func<CancellationToken, Task<Result>> work, CancellationToken cancellationToken = default) => work(cancellationToken);

    public Task<Result<T>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T>>> work, CancellationToken cancellationToken = default) => work(cancellationToken);
}
