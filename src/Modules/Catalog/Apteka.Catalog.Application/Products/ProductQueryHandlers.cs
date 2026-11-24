namespace Apteka.Catalog.Application.Products;

/// <summary>Search on the „Номенклатура“ screen: by code, part of a name or active substance, or exact barcode.</summary>
public sealed class SearchProductsHandler(IProductQueries queries)
{
    public const int DefaultLimit = 50;

    public Task<IReadOnlyList<ProductListItem>> HandleAsync(string? text, CancellationToken cancellationToken = default) =>
        HandleAsync(text, null, cancellationToken);

    /// <param name="active">true — only active („Активни“), false — only deactivated („Неактивни“), null — all.</param>
    public Task<IReadOnlyList<ProductListItem>> HandleAsync(string? text, bool? active, CancellationToken cancellationToken = default) =>
        queries.SearchAsync(string.IsNullOrWhiteSpace(text) ? null : text.Trim(), DefaultLimit, cancellationToken, active);
}

/// <summary>Loads one product for the edit form.</summary>
public sealed class GetProductHandler(IProductQueries queries)
{
    public Task<ProductView?> HandleAsync(Guid id, CancellationToken cancellationToken = default) =>
        queries.GetAsync(id, cancellationToken);
}
