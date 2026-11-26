using System.Globalization;
using System.Text;

namespace Apteka.Web.Features.Shared;

/// <summary>
/// CSV that opens straight in Excel with Bulgarian settings: „;“ between fields, a decimal comma, UTF-8 with a BOM.
/// </summary>
public static class Csv
{
    private static readonly CultureInfo Bulgarian = CultureInfo.GetCultureInfo("bg-BG");

    public static byte[] Build(IReadOnlyList<string> header, IEnumerable<IReadOnlyList<object?>> rows)
    {
        var text = new StringBuilder();
        text.AppendJoin(';', header.Select(Field)).Append("\r\n");
        foreach (var row in rows)
        {
            text.AppendJoin(';', row.Select(Format)).Append("\r\n");
        }

        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text.ToString())];
    }

    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        decimal amount => amount.ToString("0.00", Bulgarian),
        DateOnly date => date.ToString("dd.MM.yyyy", Bulgarian),
        bool flag => flag ? "да" : "не",
        IFormattable formattable => formattable.ToString(null, Bulgarian),
        _ => Field(value.ToString() ?? string.Empty),
    };

    private static string Field(string value) =>
        value.IndexOfAny([';', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
}
