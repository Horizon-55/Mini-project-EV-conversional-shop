using EVShop.Catalog.Domain.Entities;
using EVShop.Catalog.Domain.Exceptions;
using Xunit;

namespace EVShop.Catalog.Tests;

public class ProductEntityTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var product = new Product("Hyper9 Motor", "MOT-HYPER9", 4500.00m, brandId: 1, availableStock: 10);

        // Assert
        Assert.Equal("Hyper9 Motor", product.Name);
        Assert.Equal("MOT-HYPER9", product.Sku);
        Assert.Equal(4500.00m, product.Price);
        Assert.Equal(10, product.AvailableStock);
        Assert.True(product.IsAvailable);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Constructor_WithZeroOrNegativePrice_ShouldThrowDomainException(decimal invalidPrice)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Product("Inverter", "INV-100", invalidPrice));

        Assert.Contains("Price must be positive", ex.Message);
    }

    [Fact]
    public void AddStock_WithPositiveQuantity_ShouldIncreaseAvailableStock()
    {
        // Arrange
        var product = new Product("Tesla Module", "BAT-MOD-S", 1200.00m, availableStock: 5);

        // Act
        product.AddStock(3);

        // Assert
        Assert.Equal(8, product.AvailableStock);
        Assert.True(product.IsAvailable);
    }

    [Fact]
    public void RemoveStock_WithValidQuantity_ShouldDecreaseAvailableStock()
    {
        // Arrange
        var product = new Product("Tesla Module", "BAT-MOD-S", 1200.00m, availableStock: 5);

        // Act
        product.RemoveStock(2);

        // Assert
        Assert.Equal(3, product.AvailableStock);
        Assert.True(product.IsAvailable);
    }

    [Fact]
    public void RemoveStock_WhenQuantityExceedsAvailableStock_ShouldThrowDomainException()
    {
        // Arrange
        var product = new Product("Tesla Module", "BAT-MOD-S", 1200.00m, availableStock: 2);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => product.RemoveStock(5));
        Assert.Contains("Only 2 units available", ex.Message);
    }
}
