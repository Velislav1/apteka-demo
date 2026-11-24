namespace Apteka.Catalog.Domain.Products;

/// <summary>How a product may be dispensed (in Apteka Expert 2: N_DRUG_TYPE).</summary>
public enum DispensingMode
{
    /// <summary>Без рецепта.</summary>
    OverTheCounter = 1,

    /// <summary>По лекарско предписание.</summary>
    Prescription = 2,

    /// <summary>Упойващо / психотропно вещество: special prescription form and register.</summary>
    Narcotic = 3,
}
