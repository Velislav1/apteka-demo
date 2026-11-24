namespace Apteka.Purchasing.Domain.Suppliers;

/// <summary>Bulgarian company number (ЕИК / БУЛСТАТ): 9 or 13 digits with check digits.</summary>
public static class Eik
{
    private static readonly int[] Weights9First = [1, 2, 3, 4, 5, 6, 7, 8];
    private static readonly int[] Weights9Second = [3, 4, 5, 6, 7, 8, 9, 10];
    private static readonly int[] Weights13First = [2, 7, 3, 5];
    private static readonly int[] Weights13Second = [4, 9, 5, 7];

    public static bool IsValid(string eik)
    {
        if (eik.Length is not (9 or 13) || !eik.All(char.IsAsciiDigit))
        {
            return false;
        }

        if (eik[8] - '0' != CheckDigit(eik.AsSpan(0, 8), Weights9First, Weights9Second))
        {
            return false;
        }

        return eik.Length == 9 || eik[12] - '0' == CheckDigit(eik.AsSpan(8, 4), Weights13First, Weights13Second);
    }

    /// <summary>Check digit: weighted sum mod 11; if 10, again with the second weights; if still 10, the digit is 0.</summary>
    public static int CheckDigit(ReadOnlySpan<char> digits, int[] firstWeights, int[] secondWeights)
    {
        var first = WeightedSum(digits, firstWeights) % 11;
        if (first != 10)
        {
            return first;
        }

        var second = WeightedSum(digits, secondWeights) % 11;
        return second == 10 ? 0 : second;
    }

    /// <summary>Makes a valid 9-digit ЕИК from 8 digits (used for made-up demo suppliers).</summary>
    public static string Complete9(string eightDigits) =>
        eightDigits + CheckDigit(eightDigits, Weights9First, Weights9Second);

    private static int WeightedSum(ReadOnlySpan<char> digits, int[] weights)
    {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            sum += (digits[i] - '0') * weights[i];
        }

        return sum;
    }
}
