using System.Globalization;
using Apteka.Catalog.Contracts;
using Apteka.SharedKernel;

namespace Apteka.Web.Features.Shared;

/// <summary>How amounts, dates and quantities are shown: Bulgarian format, Europe/Sofia time (rules 9 and 10).</summary>
public static class Formats
{
    private static readonly CultureInfo Bulgarian = CultureInfo.GetCultureInfo("bg-BG");

    public static string Euro(decimal amount) => Money.Of(amount).ToString();

    public static string Price(decimal price) => UnitPrice.Of(price).ToString();

    public static string Date(DateOnly date) => date.ToString("dd.MM.yyyy", Bulgarian);

    public static string LocalTime(DateTimeOffset utc) => BulgarianTime.ToLocal(utc).ToString("dd.MM.yyyy HH:mm", Bulgarian);

    /// <summary>„2 × кутия + 5 × таблетка“: whole largest units first, the rest in smaller units.</summary>
    public static string Quantity(int baseQuantity, IReadOnlyList<CatalogUnit> units)
    {
        if (units.Count == 0)
        {
            return baseQuantity.ToString(Bulgarian);
        }

        var parts = new List<string>();
        var left = baseQuantity;
        foreach (var unit in units.OrderByDescending(u => u.Factor))
        {
            var count = left / unit.Factor;
            if (count > 0)
            {
                parts.Add($"{count} × {unit.Name}");
                left -= count * unit.Factor;
            }
        }

        return parts.Count == 0 ? "0" : string.Join(" + ", parts);
    }

    public static string BaseUnitName(IReadOnlyList<CatalogUnit> units) =>
        units.FirstOrDefault(u => u.Factor == 1)?.Name ?? string.Empty;
}
