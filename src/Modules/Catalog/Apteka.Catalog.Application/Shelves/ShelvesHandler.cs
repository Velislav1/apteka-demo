using Apteka.Catalog.Application.Common;
using Apteka.Catalog.Domain.Shelves;

namespace Apteka.Catalog.Application.Shelves;

/// <summary>Use cases of the „Рафтове“ list.</summary>
public sealed class ShelvesHandler(IReferenceRepository<Shelf> items)
    : ReferenceDataHandler<Shelf>(items, Shelf.Create);
