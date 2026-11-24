using Apteka.SharedKernel;

namespace Apteka.Sales.Domain.Returns;

/// <summary>Errors of „Корекция на продажба“, shown to the user in Bulgarian.</summary>
public static class ReturnErrors
{
    public static readonly Error SaleReversed = new("sales.return_sale_reversed", "Продажбата е сторнирана; няма какво да се върне.");
    public static readonly Error NothingToReturn = new("sales.return_nothing", "Въведете колко се връща поне от един ред.");
    public static readonly Error UnknownLine = new("sales.return_unknown_line", "Редът не е от тази продажба.");
    public static readonly Error HasReturns = new("sales.reverse_has_returns", "По продажбата има корекция; върнете останалото с „Корекция“, не със сторно.");
    public static readonly Error ManagersOnly = new("sales.return_managers_only", "Само управител може да коригира продажба.");

    public static Error TooMany(string product, int left, string unit) =>
        new("sales.return_too_many", $"„{product}“: може да се върнат най-много {left} {unit}.");
}
