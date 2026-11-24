using Apteka.Catalog.Application.Common;
using Apteka.Catalog.Domain.ProductGroups;

namespace Apteka.Catalog.Application.ProductGroups;

/// <summary>Use cases of the „Групи“ list.</summary>
public sealed class ProductGroupsHandler(IReferenceRepository<ProductGroup> items)
    : ReferenceDataHandler<ProductGroup>(items, ProductGroup.Create);
