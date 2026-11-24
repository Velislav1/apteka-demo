using Apteka.Catalog.Application.Common;
using Apteka.Catalog.Domain.Shelves;
using Apteka.SharedKernel;

namespace Apteka.Catalog.Application.Shelves;

/// <summary>Use cases of the „Рафтове“ list.</summary>
public sealed class ShelvesHandler(IReferenceRepository<Shelf> items, IUnitOfWork unitOfWork)
    : ReferenceDataHandler<Shelf>(items, Shelf.Create, unitOfWork);
