using Apteka.SharedKernel;

namespace Apteka.Inventory.Application.WriteOffs;

/// <summary>„Бракувания“ for a period (Sofia dates, both included), newest first.</summary>
public sealed class ListWriteOffsHandler(IWriteOffRepository writeOffs)
{
    public Task<IReadOnlyList<WriteOffListItem>> HandleAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        writeOffs.ListAsync(BulgarianTime.StartOfDayUtc(from), BulgarianTime.StartOfDayUtc(to.AddDays(1)), cancellationToken);
}

public sealed class GetWriteOffHandler(IWriteOffRepository writeOffs)
{
    public async Task<WriteOffView?> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var writeOff = await writeOffs.GetAsync(id, cancellationToken);
        return writeOff is null
            ? null
            : new WriteOffView(
                writeOff.Id,
                writeOff.Number,
                writeOff.CreatedAt,
                writeOff.UserId,
                writeOff.Reason,
                writeOff.Lines
                    .OrderBy(l => l.Position)
                    .Select(l => new WriteOffLineView(l.Position, l.ProductId, l.ProductName, l.UnitName, l.Quantity, l.BatchNo, l.ExpiryDate, l.PurchaseTotal.Amount))
                    .ToList(),
                writeOff.PurchaseTotal.Amount);
    }
}
