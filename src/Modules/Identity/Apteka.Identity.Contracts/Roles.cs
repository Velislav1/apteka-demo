namespace Apteka.Identity.Contracts;

/// <summary>Role names, the same in the login cookie and in every module's checks.</summary>
public static class Roles
{
    /// <summary>Управител: everything, including users and reversals.</summary>
    public const string Manager = "Manager";

    /// <summary>Фармацевт: daily work (till, deliveries, stock, catalog), without users and reversals.</summary>
    public const string Pharmacist = "Pharmacist";
}
