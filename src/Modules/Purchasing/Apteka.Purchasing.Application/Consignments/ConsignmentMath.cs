using Apteka.Inventory.Contracts;
using Apteka.Purchasing.Application.Payments;

namespace Apteka.Purchasing.Application.Consignments;

/// <summary>
/// A consignment is paid only for what is no longer in stock: received, less what went back to the supplier,
/// less what is still on the shelf. That covers sales, write-offs and shortages alike.
/// </summary>
public static class ConsignmentMath
{
    /// <summary>Base units the pharmacy owes for.</summary>
    public static int ChargeableBaseQuantity(ConsignmentLine line, SourceLineFlow? flow) =>
        Math.Max(0, line.ReceivedBaseQuantity - (flow?.OnHand ?? 0));

    /// <summary>Without VAT, at the line's net purchase price.</summary>
    public static decimal OwedNet(ConsignmentLine line, SourceLineFlow? flow) =>
        Round(line.NetUnitPrice * ChargeableBaseQuantity(line, flow) / line.UnitFactor);

    /// <summary>With the product's VAT, rounded per line like an invoice.</summary>
    public static decimal Owed(ConsignmentLine line, SourceLineFlow? flow, decimal vatRate) =>
        Round(OwedNet(line, flow) * (1 + (vatRate / 100m)));

    private static decimal Round(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}
