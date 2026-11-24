using Apteka.Catalog.Application.Common;
using Apteka.Catalog.Domain.Manufacturers;

namespace Apteka.Catalog.Application.Manufacturers;

/// <summary>Use cases of the „Производители“ list.</summary>
public sealed class ManufacturersHandler(IReferenceRepository<Manufacturer> items)
    : ReferenceDataHandler<Manufacturer>(items, Manufacturer.Create);
