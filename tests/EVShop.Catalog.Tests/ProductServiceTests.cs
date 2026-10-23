using AutoMapper;
using EVShop.Catalog.Bll.DTOs;
using EVShop.Catalog.Bll.Interfaces;
using EVShop.Catalog.Bll.Mapping;
using EVShop.Catalog.Bll.Services;
using EVShop.Catalog.Dal.Interfaces;
using EVShop.Catalog.Domain.Entities;
using EVShop.Catalog.Domain.Exceptions;
using Moq;
using Xunit;

namespace EVShop.Catalog.Tests;

public class ProductServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly IMapper _mapper;
    private readonly IProductService _service;

    public ProductServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<CatalogProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _service = new ProductService(_uowMock.Object, _mapper);
    }

    [Fact]
    public async Task CreateAsync_WhenSkuAlreadyExists_ShouldThrowConflictException()
    {
        // Arrange
        var input = new CreateProductDto
        {
            Name = "Duplicate Product",
            Sku = "MOT-DUPLICATE",
            Price = 1000m,
            AvailableStock = 10
        };

        _uowMock.Setup(u => u.Products.ExistsBySkuAsync(input.Sku, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _service.CreateAsync(input));

        Assert.Contains("MOT-DUPLICATE", ex.Message);
        _uowMock.Verify(u => u.Products.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        _uowMock.Setup(u => u.Products.GetWithDetailsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveStockAsync_WhenProductNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        _uowMock.Setup(u => u.Products.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _service.RemoveStockAsync(999, 5));
    }
}
