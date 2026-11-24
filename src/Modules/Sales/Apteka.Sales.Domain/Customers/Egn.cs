namespace Apteka.Sales.Domain.Customers;

/// <summary>
/// ЕГН (Bulgarian personal number): 10 digits, YYMMDD with the month +20 for 1800–1899 and +40 for 2000–2099,
/// and a check digit (weights 2,4,8,5,10,9,7,3,6; sum mod 11; 10 counts as 0).
/// Only invented numbers are ever used in this demo (rule 8).
/// </summary>
public static class Egn
{
    private static readonly int[] Weights = [2, 4, 8, 5, 10, 9, 7, 3, 6];

    public static bool IsValid(string? egn)
    {
        if (egn is not { Length: 10 } || !egn.All(char.IsAsciiDigit))
        {
            return false;
        }

        return BirthDate(egn) is not null && egn[9] - '0' == CheckDigit(egn);
    }

    /// <summary>The birth date the number encodes, or null if it is not a real date.</summary>
    public static DateOnly? BirthDate(string egn)
    {
        var year = int.Parse(egn[..2], System.Globalization.CultureInfo.InvariantCulture);
        var month = int.Parse(egn[2..4], System.Globalization.CultureInfo.InvariantCulture);
        var day = int.Parse(egn[4..6], System.Globalization.CultureInfo.InvariantCulture);
        (year, month) = month switch
        {
            > 40 => (2000 + year, month - 40),
            > 20 => (1800 + year, month - 20),
            _ => (1900 + year, month),
        };

        return month is >= 1 and <= 12 && day >= 1 && day <= DateTime.DaysInMonth(year, month)
            ? new DateOnly(year, month, day)
            : null;
    }

    /// <summary>The check digit for the first nine digits.</summary>
    public static int CheckDigit(string egn)
    {
        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            sum += (egn[i] - '0') * Weights[i];
        }

        var rest = sum % 11;
        return rest == 10 ? 0 : rest;
    }

    /// <summary>The first nine digits completed with their check digit (for invented test and seed data).</summary>
    public static string Complete(string nineDigits) => nineDigits + CheckDigit(nineDigits + "0");
}
