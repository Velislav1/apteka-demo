using Apteka.Catalog.Application.Common;
using Apteka.Catalog.Application.Products;
using Apteka.Catalog.Domain.Common;
using Apteka.Catalog.Domain.Products;
using Apteka.SharedKernel;

namespace Apteka.Web.Tests.Features.Catalog;

internal sealed class FakeProductQueries : IProductQueries
{
    public List<ProductListItem> Items { get; } = [];

    public List<string?> Searches { get; } = [];

    public Task<IReadOnlyList<ProductListItem>> SearchAsync(string? text, int limit, CancellationToken cancellationToken, bool? active = null)
    {
        Searches.Add(text);
        IReadOnlyList<ProductListItem> result = Items
            .Where(i => active is null || i.IsActive == active)
            .Where(i => text is null || i.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase) || i.Barcodes.Contains(text))
            .ToList();
        return Task.FromResult(result);
    }

    public Dictionary<Guid, ProductView> Views { get; } = [];

    public Task<ProductView?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Views.GetValueOrDefault(id));
}

internal sealed class FakeProductRepository : IProductRepository
{
    public List<Product> Added { get; } = [];

    public Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Added.SingleOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<string>> FindTakenBarcodesAsync(IEnumerable<string> codes, Guid? exceptProductId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>([]);

    public void Add(Product product) => Added.Add(product);

    public Task<Result> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success());
}

internal sealed class FakeReferenceRepository<T> : IReferenceRepository<T>
    where T : CatalogReference
{
    public List<T> Items { get; } = [];

    public Task<IReadOnlyList<ReferenceItemView>> ListAsync(bool includeInactive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReferenceItemView>>(
            Items.Where(i => includeInactive || i.IsActive).Select(i => new ReferenceItemView(i.Id, i.Name, i.IsActive)).ToList());

    public Task<T?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.SingleOrDefault(i => i.Id == id));

    public Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Any(i => i.Id != exceptId && string.Equals(i.Name, name, StringComparison.CurrentCultureIgnoreCase)));

    public void Add(T item) => Items.Add(item);

    public Task<Result> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success());
}
