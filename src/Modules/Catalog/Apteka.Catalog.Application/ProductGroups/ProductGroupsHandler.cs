using Apteka.Catalog.Application.Common;
using Apteka.Catalog.Domain.ProductGroups;
using Apteka.SharedKernel;

namespace Apteka.Catalog.Application.ProductGroups;

/// <summary>Use cases of the „Групи“ list.</summary>
public sealed class ProductGroupsHandler(IReferenceRepository<ProductGroup> items, IUnitOfWork unitOfWork)
    : ReferenceDataHandler<ProductGroup>(items, ProductGroup.Create, unitOfWork);
