using System.Data;
using AutoMapper;
using EVShop.Ordering.Bll.DTOs;
using EVShop.Ordering.Bll.Interfaces;
using EVShop.Ordering.Dal.Interfaces;
using EVShop.Ordering.Domain.Entities;
using EVShop.Ordering.Domain.Exceptions;

namespace EVShop.Ordering.Bll.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public OrderService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<OrderDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var order = await _uow.Orders.GetWithItemsAsync(id, ct);
        return order == null ? null : _mapper.Map<OrderDto>(order);
    }

    public async Task<IReadOnlyList<OrderDto>> GetByCustomerIdAsync(long customerId, CancellationToken ct = default)
    {
        var customer = await _uow.Customers.GetByIdAsync(customerId, ct);
        if (customer == null)
            throw new NotFoundException($"Клієнта з ID {customerId} не знайдено.");
        

        var orders = await _uow.Orders.GetByCustomerIdAsync(customerId, ct);
        return _mapper.Map<IReadOnlyList<OrderDto>>(orders);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto input, CancellationToken ct = default)
    {
        // 1. Бізнес-валідація: чи існує покупець
        var customer = await _uow.Customers.GetByIdAsync(input.CustomerId, ct);
        if (customer == null)
            throw new NotFoundException($"Клієнта з ID {input.CustomerId} не знайдено.");
        

        // 2. Бізнес-валідація: чи існують усі вказані деталі
        foreach (var item in input.Items)
        {
            var product = await _uow.Products.GetByIdAsync(item.ProductId, ct);
            if (product == null)
                throw new NotFoundException($"Товар із кодом {item.ProductId} відсутній у каталозі.");
            
        }

        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        try
        {
            var order = new Order
            {
                CustomerId = input.CustomerId,
                Status = "Submitted",
                CreatedBy = customer.FullName,
                Details = new OrderDetails
                {
                    ShippingAddress = input.ShippingAddress,
                    ContactPhone = input.ContactPhone,
                    TargetVehicleModel = input.TargetVehicleModel,
                    ConversionType = input.ConversionType,
                    InstallationNotes = input.InstallationNotes
                }
            };

            // Створення замовлення та 1:1 деталей через збережувану процедуру
            var orderId = await _uow.Orders.AddAsync(order, ct);

            // Додавання позицій зі знімком цін з локального довідника
            foreach (var item in input.Items)
            {
                await _uow.Orders.AddItemAsync(orderId, item.ProductId, item.Quantity, item.Discount, ct);
            }

            await _uow.CommitAsync(ct);

            var createdOrder = await _uow.Orders.GetWithItemsAsync(orderId, ct)
                ?? throw new NotFoundException($"Створене замовлення {orderId} не знайдено.");

            return _mapper.Map<OrderDto>(createdOrder);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }

    // Транзакційна бізнес-операція підтвердження замовлення
    public async Task ConfirmOrderAsync(long orderId, CancellationToken ct = default)
    {
        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var order = await _uow.Orders.GetWithItemsAsync(orderId, ct)
                ?? throw new NotFoundException($"Замовлення з номером {orderId} не знайдено.");

            if (order.Items.Count == 0)
                throw new BusinessConflictException($"Замовлення {orderId} не містить жодної позиції товару.");
            

            // 1. Зміна статусу -> StockConfirmed (допустимість перевіряє процедура usp_ConfirmOrder)
            await _uow.Orders.ConfirmAsync(orderId, ct);

            // 2. Зменшення залишків товару на складі
            // Якщо товару недостатньо -> кинеться BusinessConflictException і транзакція відкотиться в блоці catch
            foreach (var item in order.Items)
            {
                await _uow.Products.DecreaseStockAsync(item.ProductId, item.Quantity, ct);
            }

            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct); // статус та залишки повертаються назад
            throw;
        }
    }

    public async Task PayOrderAsync(long orderId, CancellationToken ct = default)
    {
        await _uow.Orders.PayAsync(orderId, ct);
    }

    public async Task ShipOrderAsync(long orderId, CancellationToken ct = default)
    {
        await _uow.Orders.ShipAsync(orderId, ct);
    }

    public async Task CancelOrderAsync(long orderId, string reason = "Скасовано клієнтом", CancellationToken ct = default)
    {
        await _uow.Orders.CancelAsync(orderId, reason, ct);
    }

    public async Task AddItemAsync(long orderId, CreateOrderItemDto itemInput, CancellationToken ct = default)
    {
        var product = await _uow.Products.GetByIdAsync(itemInput.ProductId, ct);
        if (product == null)
            throw new NotFoundException($"Товар із кодом {itemInput.ProductId} не знайдено.");
        

        await _uow.Orders.AddItemAsync(orderId, itemInput.ProductId, itemInput.Quantity, itemInput.Discount, ct);
    }
}
