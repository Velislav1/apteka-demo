using Apteka.Catalog.Contracts;
using Apteka.Inventory.Contracts;
using Apteka.Purchasing.Application.Suppliers;
using Apteka.Purchasing.Domain.Orders;
using Apteka.SharedKernel;

namespace Apteka.Purchasing.Application.Orders;

/// <summary>Proposes what to order, by the chosen criterion, for active products (optionally of one group).</summary>
public sealed class SuggestOrderHandler(ICatalogLookup catalog, IStockQueries stock, Refusals.IRefusalRepository refusals, TimeProvider clock)
{
    public const int MaxDays = 365;

    public async Task<Result<IReadOnlyList<OrderSuggestion>>> HandleAsync(
        OrderCriterion criterion,
        int days,
        Guid? groupId,
        CancellationToken cancellationToken = default)
    {
        if (criterion == OrderCriterion.Manual)
        {
            return Result.Success<IReadOnlyList<OrderSuggestion>>([]);
        }

        if (criterion != OrderCriterion.MinStock && days is < 1 or > MaxDays)
        {
            return Result.Failure<IReadOnlyList<OrderSuggestion>>(OrderErrors.InvalidDays);
        }

        var products = await catalog.ListProductsAsync(groupId, withMinStockOnly: criterion == OrderCriterion.MinStock, cancellationToken);
        var ids = products.Select(p => p.ProductId).ToList();
        var onHand = (await stock.GetProductStockAsync(ids, null, cancellationToken)).ToDictionary(s => s.ProductId, s => s.BaseQuantity);
        var now = clock.GetUtcNow();
        var sold = criterion == OrderCriterion.MinStock
            ? new Dictionary<Guid, int>()
            : new Dictionary<Guid, int>(await stock.GetSoldQuantitiesAsync(ids, now.AddDays(-days), now, cancellationToken));

        // Refusals are demand too (asked for and not sold because it was missing).
        if (criterion != OrderCriterion.MinStock)
        {
            foreach (var refusal in (await refusals.ListAsync(now.AddDays(-days), now, cancellationToken)).Where(r => r.ProductId is not null))
            {
                sold[refusal.ProductId!.Value] = sold.GetValueOrDefault(refusal.ProductId.Value) + refusal.Quantity;
            }
        }

        var result = new List<OrderSuggestion>();
        foreach (var product in products)
        {
            var have = onHand.GetValueOrDefault(product.ProductId);
            var went = sold.GetValueOrDefault(product.ProductId);
            var need = criterion switch
            {
                OrderCriterion.MinStock when have < product.MinStock => (product.MaxStock ?? product.MinStock!.Value) - have,
                OrderCriterion.Sales => went,
                OrderCriterion.SalesLessStock => went - have,
                _ => 0,
            };

            if (need <= 0 || product.Units.Count == 0)
            {
                continue;
            }

            var unit = product.Units.MaxBy(u => u.Factor)!;
            var quantity = (need + unit.Factor - 1) / unit.Factor;
            result.Add(new OrderSuggestion(product.ProductId, product.Code, product.Name, unit.UnitId, unit.Name, unit.Factor, quantity, have, went, product.Units));
        }

        return Result.Success<IReadOnlyList<OrderSuggestion>>(result);
    }
}

/// <summary>„Заявки за доставка“: save a draft, mark it sent, list and open.</summary>
public sealed class OrdersHandler(
    IPurchaseOrderRepository orders,
    ISupplierRepository suppliers,
    ICatalogLookup catalog,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public const int ListLimit = 100;

    public Task<IReadOnlyList<PurchaseOrderListItem>> ListAsync(CancellationToken cancellationToken = default) =>
        orders.ListAsync(ListLimit, cancellationToken);

    /// <param name="id">Null creates a new order.</param>
    public Task<Result<Guid>> SaveAsync(Guid? id, OrderInput input, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            if (await suppliers.GetAsync(input.SupplierId, ct) is not { IsActive: true })
            {
                return Result.Failure<Guid>(OrderErrors.SupplierNotAvailable);
            }

            var products = (await catalog.GetProductsAsync([.. input.Lines.Select(l => l.ProductId).Distinct()], ct)).ToDictionary(p => p.ProductId);
            var specs = new List<PurchaseOrderLineSpec>();
            for (var i = 0; i < input.Lines.Count; i++)
            {
                var line = input.Lines[i];
                var unit = products.GetValueOrDefault(line.ProductId)?.Units.FirstOrDefault(u => u.UnitId == line.UnitId);
                if (unit is null)
                {
                    return Result.Failure<Guid>(OrderErrors.InvalidQuantity(i + 1));
                }

                specs.Add(new PurchaseOrderLineSpec(line.ProductId, line.UnitId, unit.Factor, line.Quantity));
            }

            PurchaseOrder order;
            if (id is { } existingId)
            {
                if (await orders.GetAsync(existingId, ct) is not { } existing)
                {
                    return Result.Failure<Guid>(OrderErrors.NotFound);
                }

                var updated = existing.Update(input.SupplierId, input.Note, specs);
                if (updated.IsFailure)
                {
                    return Result.Failure<Guid>(updated.Error);
                }

                order = existing;
            }
            else
            {
                var created = PurchaseOrder.Create(input.SupplierId, input.Note, specs, currentUser.UserId, clock.GetUtcNow());
                if (created.IsFailure)
                {
                    return Result.Failure<Guid>(created.Error);
                }

                order = created.Value;
                orders.Add(order);
            }

            await orders.SaveChangesAsync(ct);
            return Result.Success(order.Id);
        }, cancellationToken);

    public Task<Result> MarkSentAsync(Guid id, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            if (await orders.GetAsync(id, ct) is not { } order)
            {
                return Result.Failure(OrderErrors.NotFound);
            }

            var sent = order.MarkSent(clock.GetUtcNow());
            if (sent.IsSuccess)
            {
                await orders.SaveChangesAsync(ct);
            }

            return sent;
        }, cancellationToken);

    public async Task<OrderView?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await orders.GetAsync(id, cancellationToken) is not { } order)
        {
            return null;
        }

        var supplier = await suppliers.GetAsync(order.SupplierId, cancellationToken);
        var products = (await catalog.GetProductsAsync([.. order.Lines.Select(l => l.ProductId).Distinct()], cancellationToken)).ToDictionary(p => p.ProductId);
        return new OrderView(
            order.Id,
            order.Number,
            order.SupplierId,
            supplier?.Name ?? "?",
            order.Note,
            order.Status,
            order.CreatedAt,
            order.SentAt,
            order.Lines.OrderBy(l => l.Position).Select(l =>
            {
                var product = products.GetValueOrDefault(l.ProductId);
                return new OrderLineView(
                    l.ProductId,
                    product?.Code ?? 0,
                    product?.Name ?? "?",
                    l.ProductUnitId,
                    product?.Units.FirstOrDefault(u => u.UnitId == l.ProductUnitId)?.Name ?? string.Empty,
                    l.Quantity,
                    product?.Units ?? []);
            }).ToList());
    }
}
