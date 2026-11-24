using Apteka.Catalog.Application.Products;
using Apteka.Catalog.Domain.Products;
using Apteka.SharedKernel;

namespace Apteka.Web.Tests.Features.Catalog;

internal sealed class FakeProductQueries : IProductQueries
{
    public List<ProductListItem> Items { get; } = [];

    public List<string?> Searches { get; } = [];

    public Task<IReadOnlyList<ProductListItem>> SearchAsync(string? text, int limit, CancellationToken cancellationToken)
    {
        Searches.Add(text);
        IReadOnlyList<ProductListItem> result = Items
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
