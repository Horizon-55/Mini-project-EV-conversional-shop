using AutoMapper;
using EVShop.Ordering.Bll.Mapping;
using EVShop.Ordering.Bll.Services;
using EVShop.Ordering.Dal.Interfaces;
using EVShop.Ordering.Domain.Entities;
using EVShop.Ordering.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Data;
using Xunit;

namespace EVShop.Ordering.Tests;

public class OrderServiceTests
{
    private readonly IMapper _mapper;

    public OrderServiceTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<OrderProfile>();
        }, NullLoggerFactory.Instance);

        // Перевірка коректності конфігурації мапінгу
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void AutoMapper_Configuration_IsValid()
    {
        // Assert здійснюється в конструкторі при виклику AssertConfigurationIsValid
        Assert.NotNull(_mapper);
    }

    [Fact]
    public async Task ConfirmOrderAsync_WhenOrderNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var uowMock = new Mock<IUnitOfWork>();
        var orderRepoMock = new Mock<IOrderRepository>();

        orderRepoMock
            .Setup(r => r.GetWithItemsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        uowMock.SetupGet(u => u.Orders).Returns(orderRepoMock.Object);

        var service = new OrderService(uowMock.Object, _mapper);

        // Act & Assert 
        await Assert.ThrowsAsync<NotFoundException>(() => service.ConfirmOrderAsync(99999));
        uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmOrderAsync_WhenOrderHasNoItems_ThrowsBusinessConflictException()
    {
        // Arrange
        var uowMock = new Mock<IUnitOfWork>();
        var orderRepoMock = new Mock<IOrderRepository>();

        var emptyOrder = new Order
        {
            Id = 101,
            CustomerId = 1,
            Status = "Submitted",
            Items = new List<OrderItem>()
        };

        orderRepoMock
            .Setup(r => r.GetWithItemsAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyOrder);

        uowMock.SetupGet(u => u.Orders).Returns(orderRepoMock.Object);

        var service = new OrderService(uowMock.Object, _mapper);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessConflictException>(() => service.ConfirmOrderAsync(101));
        uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmOrderAsync_WhenDecreaseStockFails_RollsBackTransaction()
    {
        // Arrange
        var uowMock = new Mock<IUnitOfWork>();
        var orderRepoMock = new Mock<IOrderRepository>();
        var productRepoMock = new Mock<IProductRepository>();

        var order = new Order
        {
            Id = 202,
            CustomerId = 1,
            Status = "Submitted",
            Items = new List<OrderItem>
            {
                new() { OrderId = 202, ProductId = 5001, ProductName = "Nissan EM57 Motor", UnitPrice = 1400, Quantity = 10 }
            }
        };

        orderRepoMock
            .Setup(r => r.GetWithItemsAsync(202, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        orderRepoMock
            .Setup(r => r.ConfirmAsync(202, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        productRepoMock
            .Setup(p => p.DecreaseStockAsync(5001, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessConflictException("Недостатньо залишку на складі"));

        uowMock.SetupGet(u => u.Orders).Returns(orderRepoMock.Object);
        uowMock.SetupGet(u => u.Products).Returns(productRepoMock.Object);

        var service = new OrderService(uowMock.Object, _mapper);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessConflictException>(() => service.ConfirmOrderAsync(202));

        // Гарантуємо відкат транзакції (Вимога B.4, Рис. 2)
        uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmOrderAsync_WhenSuccessful_CommitsTransaction()
    {
        // Arrange
        var uowMock = new Mock<IUnitOfWork>();
        var orderRepoMock = new Mock<IOrderRepository>();
        var productRepoMock = new Mock<IProductRepository>();

        var order = new Order
        {
            Id = 303,
            CustomerId = 1,
            Status = "Submitted",
            Items = new List<OrderItem>
            {
                new() { OrderId = 303, ProductId = 5001, ProductName = "Nissan EM57 Motor", UnitPrice = 1400, Quantity = 1 }
            }
        };

        orderRepoMock
            .Setup(r => r.GetWithItemsAsync(303, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        orderRepoMock
            .Setup(r => r.ConfirmAsync(303, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        productRepoMock
            .Setup(p => p.DecreaseStockAsync(5001, 1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        uowMock.SetupGet(u => u.Orders).Returns(orderRepoMock.Object);
        uowMock.SetupGet(u => u.Products).Returns(productRepoMock.Object);

        var service = new OrderService(uowMock.Object, _mapper);

        // Act
        await service.ConfirmOrderAsync(303);

        // Assert
        uowMock.Verify(u => u.BeginTransactionAsync(IsolationLevel.ReadCommitted, It.IsAny<CancellationToken>()), Times.Once);
        orderRepoMock.Verify(r => r.ConfirmAsync(303, It.IsAny<CancellationToken>()), Times.Once);
        productRepoMock.Verify(p => p.DecreaseStockAsync(5001, 1, It.IsAny<CancellationToken>()), Times.Once);
        uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        uowMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
