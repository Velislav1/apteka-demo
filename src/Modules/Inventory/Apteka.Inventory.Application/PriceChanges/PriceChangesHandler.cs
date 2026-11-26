using Apteka.Catalog.Contracts;
using Apteka.Inventory.Contracts;
using Apteka.Inventory.Domain.Batches;
using Apteka.Inventory.Domain.PriceChanges;
using Apteka.SharedKernel;

namespace Apteka.Inventory.Application.PriceChanges;

public interface IPriceChangeRepository
{
    void Add(PriceChange change);

    /// <summary>The batches, tracked for changes.</summary>
    Task<IReadOnlyList<Batch>> GetBatchesAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken);

    /// <summary>Newest first.</summary>
    Task<IReadOnlyList<PriceChange>> ListAsync(int limit, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>How the new prices are proposed.</summary>
public enum PriceMethod
{
    /// <summary>Old price ± a percent.</summary>
    Percent = 1,

    /// <summary>Purchase price × (1 + markup %) × (1 + VAT %): a new markup on what the batch cost.</summary>
    Markup = 2,
}

/// <param name="GroupId">Null — every product with stock (or only <paramref name="ProductIds"/>).</param>
/// <param name="RoundTo">Step the new prices are rounded to (0,01; 0,05; 0,10 €).</param>
public sealed record PriceProposal(Guid? GroupId, IReadOnlyCollection<Guid>? ProductIds, PriceMethod Method, decimal Value, decimal RoundTo = 0.01m);

/// <param name="PurchasePrice">Purchase price of the priced unit, without VAT.</param>
public sealed record PriceRow(
    Guid BatchId,
    Guid ProductId,
    long Code,
    string ProductName,
    string BatchNo,
    DateOnly ExpiryDate,
    string UnitName,
    int PricedUnitFactor,
    int BaseQuantity,
    decimal VatRate,
    decimal PurchasePrice,
    decimal OldPrice,
    decimal NewPrice)
{
    /// <summary>Markup of a price on the purchase price, without VAT, in %.</summary>
    public decimal? MarkupOf(decimal price) =>
        PurchasePrice == 0 ? null : decimal.Round(((price / (1 + (VatRate / 100m))) / PurchasePrice - 1) * 100m, 1, MidpointRounding.AwayFromZero);
}

public sealed record PriceChangeItem(Guid Id, long Number, DateTimeOffset CreatedAt, Guid? UserId, string? Reason, int LineCount);

/// <summary>
/// „Промяна на цени“: proposes new sale prices for the batches in stock (by a percent or a new markup, for a group or
/// chosen products), lets every price be corrected by hand, and applies them in one transaction with a numbered record.
/// </summary>
public sealed class PriceChangesHandler(
    ICatalogLookup catalog,
    IStockQueries stock,
    IPriceChangeRepository changes,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public const int ListLimit = 100;

    public async Task<IReadOnlyList<PriceRow>> ProposeAsync(PriceProposal proposal, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Guid>? ids = proposal.ProductIds;
        if (proposal.GroupId is { } group)
        {
            var inGroup = (await catalog.ListProductsAsync(group, withMinStockOnly: false, cancellationToken)).Select(p => p.ProductId).ToList();
            ids = ids is null ? inGroup : ids.Intersect(inGroup).ToList();
        }

        var batches = await stock.GetAllBatchesAsync(ids, null, cancellationToken);
        var products = (await catalog.GetProductsAsync([.. batches.Select(b => b.ProductId).Distinct()], cancellationToken)).ToDictionary(p => p.ProductId);
        var step = proposal.RoundTo <= 0 ? 0.01m : proposal.RoundTo;

        return batches
            .Where(b => products.ContainsKey(b.ProductId))
            .Select(b =>
            {
                var product = products[b.ProductId];
                var batch = b.Batch;
                var purchase = decimal.Round(batch.PurchasePricePerBaseUnit * batch.PricedUnitFactor, 4, MidpointRounding.AwayFromZero);
                var proposed = proposal.Method == PriceMethod.Markup
                    ? purchase * (1 + (proposal.Value / 100m)) * (1 + (product.VatRate / 100m))
                    : batch.PricedUnitSalePrice * (1 + (proposal.Value / 100m));
                var rounded = Math.Max(step, decimal.Round(proposed / step, 0, MidpointRounding.AwayFromZero) * step);
                return new PriceRow(
                    batch.BatchId,
                    product.ProductId,
                    product.Code,
                    product.Name,
                    batch.BatchNo,
                    batch.ExpiryDate,
                    product.Units.FirstOrDefault(u => u.Factor == batch.PricedUnitFactor)?.Name ?? batch.PricedUnitFactor.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    batch.PricedUnitFactor,
                    batch.BaseQuantity,
                    product.VatRate,
                    purchase,
                    batch.PricedUnitSalePrice,
                    rounded);
            })
            .OrderBy(r => r.ProductName)
            .ThenBy(r => r.ExpiryDate)
            .ToList();
    }

    public Task<Result<long>> ApplyAsync(string? reason, IReadOnlyDictionary<Guid, decimal> newPrices, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var batches = (await changes.GetBatchesAsync([.. newPrices.Keys], ct)).ToDictionary(b => b.Id);
            if (newPrices.Keys.Any(id => !batches.ContainsKey(id)))
            {
                return Result.Failure<long>(PriceChangeErrors.UnknownBatch);
            }

            var created = PriceChange.Create(
                reason,
                newPrices.Select(p => new PriceChangeLineSpec(p.Key, batches[p.Key].ProductId, batches[p.Key].PricedUnitFactor, batches[p.Key].PricedUnitSalePrice, decimal.Round(p.Value, 2, MidpointRounding.AwayFromZero))).ToList(),
                currentUser.UserId,
                clock.GetUtcNow());
            if (created.IsFailure)
            {
                return Result.Failure<long>(created.Error);
            }

            foreach (var line in created.Value.Lines)
            {
                var changed = batches[line.BatchId].ChangeSalePrice(line.NewPrice);
                if (changed.IsFailure)
                {
                    return Result.Failure<long>(changed.Error);
                }
            }

            changes.Add(created.Value);
            await changes.SaveChangesAsync(ct);
            return Result.Success(created.Value.Number);
        }, cancellationToken);

    public async Task<IReadOnlyList<PriceChangeItem>> ListAsync(CancellationToken cancellationToken = default) =>
        (await changes.ListAsync(ListLimit, cancellationToken))
            .Select(c => new PriceChangeItem(c.Id, c.Number, c.CreatedAt, c.UserId, c.Reason, c.Lines.Count))
            .ToList();
}
