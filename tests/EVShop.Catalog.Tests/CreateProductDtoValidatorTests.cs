using EVShop.Catalog.Bll.DTOs;
using EVShop.Catalog.Bll.Validators;
using Xunit;

namespace EVShop.Catalog.Tests;

public class CreateProductDtoValidatorTests
{
    private readonly CreateProductDtoValidator _validator = new();

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "",
            Sku = "MOT-123",
            Price = 1000m,
            AvailableStock = 5
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductDto.Name));
    }

    [Fact]
    public async Task Validate_WhenPriceIsZeroOrNegative_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Inverter",
            Sku = "INV-100",
            Price = 0,
            AvailableStock = 5
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductDto.Price));
    }

    [Theory]
    [InlineData("SKU WITH SPACES")]
    [InlineData("SKU#SPECIAL!")]
    public async Task Validate_WhenSkuContainsInvalidCharacters_ShouldFailValidation(string invalidSku)
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Inverter",
            Sku = invalidSku,
            Price = 1000m,
            AvailableStock = 5
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductDto.Sku));
    }

    [Fact]
    public async Task Validate_WhenValidInput_ShouldPassValidation()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Hyper9 Synchronous AC Motor",
            Sku = "MOT-HYPER9-100",
            Price = 4500.00m,
            AvailableStock = 10,
            OperatingVoltageV = 100
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        Assert.True(result.IsValid);
    }
}
