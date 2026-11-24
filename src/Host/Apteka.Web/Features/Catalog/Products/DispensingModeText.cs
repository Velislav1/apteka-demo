using Apteka.Catalog.Domain.Products;

namespace Apteka.Web.Features.Catalog.Products;

/// <summary>Bulgarian labels for <see cref="DispensingMode"/>.</summary>
public static class DispensingModeText
{
    public static string Label(DispensingMode mode) => mode switch
    {
        DispensingMode.OverTheCounter => "Без рецепта",
        DispensingMode.Prescription => "По лекарско предписание",
        DispensingMode.Narcotic => "Упойващо (специална рецепта)",
        _ => mode.ToString(),
    };

    public static string? Tag(DispensingMode mode) => mode switch
    {
        DispensingMode.Prescription => "Rx",
        DispensingMode.Narcotic => "Опиат",
        _ => null,
    };
}
