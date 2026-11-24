namespace Apteka.Catalog.Domain.Products;

/// <summary>GTIN barcodes (EAN-8, UPC-A, EAN-13, GTIN-14): digits only, with a valid check digit.</summary>
public static class Gtin
{
    public static bool IsValid(string code)
    {
        if (code.Length is not (8 or 12 or 13 or 14) || !code.All(char.IsAsciiDigit))
        {
            return false;
        }

        return code[^1] - '0' == CheckDigit(code.AsSpan(0, code.Length - 1));
    }

    /// <summary>Check digit for the given digits (without the check digit itself).</summary>
    public static int CheckDigit(ReadOnlySpan<char> digits)
    {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            // Weights 3,1,3,1... counted from the rightmost digit.
            var weight = (digits.Length - i) % 2 == 1 ? 3 : 1;
            sum += (digits[i] - '0') * weight;
        }

        return (10 - (sum % 10)) % 10;
    }
}
