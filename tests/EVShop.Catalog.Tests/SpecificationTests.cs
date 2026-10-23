using EVShop.Catalog.Bll.DTOs;
using EVShop.Catalog.Bll.Specifications;
using EVShop.Catalog.Domain.Entities;
using Xunit;

namespace EVShop.Catalog.Tests;

public class SpecificationTests
{
    [Fact]
    public void ProductsFilterSpec_WithPagingAndFiltering_ShouldConfigureSpecCorrectly()
    {
        // Arrange
        var query = new ProductQuery(
            Page: 2,
            PageSize: 10,
            Search: "Hyper9",
            MinPrice: 1000m,
            MaxPrice: 5000m,
            SortBy: "price",
            SortDir: "desc"
        );

        // Act
        var spec = new ProductsFilterSpec(query, pageSize: 10);

        // Assert
        Assert.NotNull(spec);
        Assert.Equal(10, spec.Take);
        Assert.Equal(10, spec.Skip); // (2 - 1) * 10 = 10
    }

    [Fact]
    public void ProductsCountSpec_ShouldNotHavePaging()
    {
        // Arrange
        var query = new ProductQuery(
            Page: 3,
            PageSize: 50,
            Search: "Curtis"
        );

        // Act
        var spec = new ProductsCountSpec(query);

        // Assert
        Assert.NotNull(spec);
        Assert.Equal(-1, spec.Take);
        Assert.Equal(-1, spec.Skip);
    }
}
