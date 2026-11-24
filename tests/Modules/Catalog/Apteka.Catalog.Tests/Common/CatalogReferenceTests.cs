using Apteka.Catalog.Domain.Common;
using Apteka.Catalog.Domain.Manufacturers;
using Apteka.Catalog.Domain.ProductGroups;
using Apteka.Catalog.Domain.Shelves;

namespace Apteka.Catalog.Tests.Common;

public class CatalogReferenceTests
{
    [Fact]
    public void New_items_are_active_with_a_trimmed_name()
    {
        var manufacturer = Manufacturer.Create("  Демо Фарма АД ").Value;

        Assert.Equal("Демо Фарма АД", manufacturer.Name);
        Assert.True(manufacturer.IsActive);
        Assert.NotEqual(Guid.Empty, manufacturer.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Name_is_required_for_every_list(string name)
    {
        Assert.Equal(ReferenceErrors.NameRequired, Manufacturer.Create(name).Error);
        Assert.Equal(ReferenceErrors.NameRequired, ProductGroup.Create(name).Error);
        Assert.Equal(ReferenceErrors.NameRequired, Shelf.Create(name).Error);
    }

    [Fact]
    public void Too_long_name_is_rejected()
    {
        Assert.Equal(ReferenceErrors.NameTooLong, Shelf.Create(new string('а', CatalogReference.MaxNameLength + 1)).Error);
    }

    [Fact]
    public void Item_is_deactivated_not_deleted_and_failed_update_changes_nothing()
    {
        var group = ProductGroup.Create("Витамини").Value;

        Assert.True(group.Update("Витамини и минерали", isActive: false).IsSuccess);
        Assert.False(group.IsActive);

        Assert.True(group.Update(" ", isActive: true).IsFailure);
        Assert.Equal("Витамини и минерали", group.Name);
        Assert.False(group.IsActive);
    }

    [Fact]
    public void Group_markup_can_be_set_and_cleared_within_limits()
    {
        var group = ProductGroup.Create("Витамини").Value;

        Assert.True(group.SetMarkup(18m).IsSuccess);
        Assert.Equal(18m, group.MarkupPercent);
        Assert.True(group.SetMarkup(null).IsSuccess);
        Assert.Null(group.MarkupPercent);
        Assert.Equal(Apteka.Catalog.Domain.Products.CatalogErrors.InvalidMarkup, group.SetMarkup(-5m).Error);
    }
}
