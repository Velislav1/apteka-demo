using Apteka.Inventory.Domain.Stocktakings;

namespace Apteka.Inventory.Application.Stocktakings;

/// <summary>What to count: one group, one shelf, both, or (both null) all products with stock.</summary>
public sealed record StartStocktakingInput(Guid? GroupId, Guid? ShelfId);

public sealed record StocktakingView(
    Guid Id,
    long Number,
    string Scope,
    StocktakingStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<StocktakingLineView> Lines,
    decimal DifferenceValue);

public sealed record StocktakingLineView(
    Guid Id,
    int Position,
    Guid ProductId,
    string ProductName,
    string BatchNo,
    DateOnly ExpiryDate,
    int ExpectedQuantity,
    int? CountedQuantity,
    int? Difference,
    decimal DifferenceValue);
