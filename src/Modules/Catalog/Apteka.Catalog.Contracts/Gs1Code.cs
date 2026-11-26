using System.Globalization;

namespace Apteka.Catalog.Contracts;

/// <summary>
/// What a GS1 DataMatrix (the 2D code on medicine packs, Falsified Medicines Directive) carries:
/// (01) GTIN, (17) expiry YYMMDD, (10) batch, (21) serial number. A scanner sends the fields one after another;
/// the variable ones (10, 21) end with the GS character (ASCII 29) or at the end of the text.
/// Scanners in keyboard mode often send GS as „|“ or „~“, and people type the readable form „(01)…(17)…“; all are read.
/// </summary>
public sealed record Gs1Code(string Gtin, string? BatchNo, DateOnly? ExpiryDate, string? SerialNumber)
{
    private const char GroupSeparator = '\u001d';

    /// <summary>The GTIN as an EAN-13 (how barcodes are stored), when it starts with 0; otherwise the 14 digits.</summary>
    public string Ean13 => Gtin.Length == 14 && Gtin[0] == '0' ? Gtin[1..] : Gtin;

    /// <summary>True for text that can only be a GS1 element string, not a plain barcode or a name.</summary>
    public static bool LooksLikeGs1(string text)
    {
        var t = text.Trim();
        return t.StartsWith("]d2", StringComparison.Ordinal)
            || t.StartsWith("(01)", StringComparison.Ordinal)
            || (t.Length > 16 && t.StartsWith("01", StringComparison.Ordinal) && t[2..16].All(char.IsAsciiDigit));
    }

    /// <summary>The fields of the code, or null when it has no valid GTIN.</summary>
    public static Gs1Code? Parse(string text)
    {
        var s = Normalize(text);
        string? gtin = null, batch = null, serial = null;
        DateOnly? expiry = null;
        var i = 0;
        while (i < s.Length)
        {
            if (s[i] == GroupSeparator)
            {
                i++;
                continue;
            }

            if (i + 2 > s.Length)
            {
                return null;
            }

            var ai = s.Substring(i, 2);
            i += 2;
            switch (ai)
            {
                case "01":
                    if (i + 14 > s.Length || !s.Substring(i, 14).All(char.IsAsciiDigit))
                    {
                        return null;
                    }

                    gtin = s.Substring(i, 14);
                    i += 14;
                    break;
                case "17":
                    if (i + 6 > s.Length)
                    {
                        return null;
                    }

                    expiry = ExpiryFrom(s.Substring(i, 6));
                    i += 6;
                    break;
                case "10":
                    batch = Variable(s, ref i, 20);
                    break;
                case "21":
                    serial = Variable(s, ref i, 20);
                    break;
                case "71":
                    // National healthcare reimbursement number (712 in Bulgaria and others): read and ignore.
                    i++;
                    Variable(s, ref i, 20);
                    break;
                default:
                    return gtin is null ? null : new Gs1Code(gtin, batch, expiry, serial);
            }
        }

        return gtin is not null && IsValidGtin(gtin) ? new Gs1Code(gtin, batch, expiry, serial) : null;
    }

    /// <summary>YYMMDD; day 00 means the last day of the month.</summary>
    private static DateOnly? ExpiryFrom(string yymmdd)
    {
        if (!yymmdd.All(char.IsAsciiDigit))
        {
            return null;
        }

        var year = 2000 + int.Parse(yymmdd[..2], CultureInfo.InvariantCulture);
        var month = int.Parse(yymmdd[2..4], CultureInfo.InvariantCulture);
        var day = int.Parse(yymmdd[4..], CultureInfo.InvariantCulture);
        if (month is < 1 or > 12)
        {
            return null;
        }

        var last = DateTime.DaysInMonth(year, month);
        return day == 0 ? new DateOnly(year, month, last) : day <= last ? new DateOnly(year, month, day) : null;
    }

    private static string Variable(string s, ref int i, int maxLength)
    {
        var end = s.IndexOf(GroupSeparator, i);
        if (end < 0 || end - i > maxLength)
        {
            end = Math.Min(s.Length, i + maxLength);
            if (s.IndexOf(GroupSeparator, i) is var gs and >= 0 && gs < end)
            {
                end = gs;
            }
        }

        var value = s[i..end];
        i = end;
        return value;
    }

    /// <summary>Strips the symbology prefix, turns „(01)…(17)…“ and GS stand-ins into the raw form.</summary>
    private static string Normalize(string text)
    {
        var s = text.Trim();
        if (s.StartsWith("]d2", StringComparison.Ordinal))
        {
            s = s[3..];
        }

        s = s.Replace('|', GroupSeparator).Replace('~', GroupSeparator);
        if (s.StartsWith('('))
        {
            // „(01)05901234123457(17)271231(10)ABC“: a field ends where the next „(“ starts.
            s = s.Replace(")", string.Empty, StringComparison.Ordinal).Replace("(", GroupSeparator.ToString(), StringComparison.Ordinal);
        }

        return s;
    }

    private static bool IsValidGtin(string gtin)
    {
        var sum = 0;
        for (var i = 0; i < 13; i++)
        {
            var digit = gtin[i] - '0';
            sum += (i % 2 == 0) ? digit * 3 : digit;
        }

        return (10 - (sum % 10)) % 10 == gtin[13] - '0';
    }
}
