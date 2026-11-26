using Apteka.Inventory.Domain.WriteOffs;

namespace Apteka.Web.Features.Inventory.WriteOffs;

/// <summary>Names and addresses of the kinds of „изписване“ (one menu item each, as in Apteka Expert 2).</summary>
public static class WriteOffKinds
{
    public static string Title(WriteOffKind kind) => kind switch
    {
        WriteOffKind.Sample => "Изписване за мостра",
        WriteOffKind.Protocol => "Изписване с протокол",
        WriteOffKind.Shortage => "Изписване на липси",
        _ => "Бракуване на стоки",
    };

    public static string Short(WriteOffKind kind) => kind switch
    {
        WriteOffKind.Sample => "мостра",
        WriteOffKind.Protocol => "протокол",
        WriteOffKind.Shortage => "липси",
        _ => "брак",
    };

    /// <summary>The „?kind=“ value in the address.</summary>
    public static string Slug(WriteOffKind kind) => kind.ToString().ToLowerInvariant();

    public static WriteOffKind Parse(string? slug) =>
        Enum.TryParse<WriteOffKind>(slug, ignoreCase: true, out var kind) && Enum.IsDefined(kind) ? kind : WriteOffKind.WriteOff;
}
