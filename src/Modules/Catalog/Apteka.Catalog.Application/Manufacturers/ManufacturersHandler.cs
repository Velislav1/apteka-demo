using Apteka.Catalog.Application.Common;
using Apteka.Catalog.Domain.Manufacturers;
using Apteka.SharedKernel;

namespace Apteka.Catalog.Application.Manufacturers;

/// <summary>Use cases of the „Производители“ list.</summary>
public sealed class ManufacturersHandler(IReferenceRepository<Manufacturer> items, IUnitOfWork unitOfWork)
    : ReferenceDataHandler<Manufacturer>(items, Manufacturer.Create, unitOfWork);
