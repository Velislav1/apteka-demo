using Apteka.Catalog.Contracts;
using Apteka.Purchasing.Domain.Orders;

namespace Apteka.Purchasing.Application.Orders;

/// <summary>„Критерий за поръчано количество“ as in Apteka Expert 2.</summary>
public enum OrderCriterion
{
    /// <summary>Ръчно попълване: no proposal.</summary>
    Manual = 1,

    /// <summary>Според минимален запас: products below their minimum, up to the maximum (or the minimum).</summary>
    MinStock = 2,

    /// <summary>Според продажбите за N дни: what sold in the last N days, for the next N days.</summary>
    Sales = 3,

    /// <summary>Според продажбите и наличността за N дни: what sold in the last N days less what is on hand.</summary>
    SalesLessStock = 4,
}

/// <param name="Quantity">Proposed, in <paramref name="UnitId"/> (the largest unit), rounded up.</param>
/// <param name="OnHand">Stock now, in base units.</param>
/// <param name="Sold">Sold in the period, in base units (0 for the minimum-stock criterion).</param>
public sealed record OrderSuggestion(
    Guid ProductId,
    long Code,
    string Name,
    Guid UnitId,
    string UnitName,
    int UnitFactor,
    int Quantity,
    int OnHand,
    int Sold,
    IReadOnlyList<CatalogUnit> Units);

public sealed record OrderInput(Guid SupplierId, string? Note, IReadOnlyList<OrderLineInput> Lines);

/// <param name="Quantity">In <paramref name="UnitId"/>.</param>
public sealed record OrderLineInput(Guid ProductId, Guid UnitId, int Quantity);

public sealed record OrderView(
    Guid Id,
    long Number,
    Guid SupplierId,
    string SupplierName,
    string? Note,
    PurchaseOrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt,
    IReadOnlyList<OrderLineView> Lines);

public sealed record OrderLineView(Guid ProductId, long Code, string ProductName, Guid UnitId, string UnitName, int Quantity, IReadOnlyList<CatalogUnit> Units);
