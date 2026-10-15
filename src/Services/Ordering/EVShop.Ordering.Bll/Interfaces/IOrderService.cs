using EVShop.Ordering.Bll.DTOs;

namespace EVShop.Ordering.Bll.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderDto input, CancellationToken ct = default);
    Task<OrderDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<OrderDto>> GetByCustomerIdAsync(long customerId, CancellationToken ct = default);
    Task ConfirmOrderAsync(long orderId, CancellationToken ct = default);
    Task PayOrderAsync(long orderId, CancellationToken ct = default);
    Task ShipOrderAsync(long orderId, CancellationToken ct = default);
    Task CancelOrderAsync(long orderId, string reason = "Скасовано клієнтом", CancellationToken ct = default);
    Task AddItemAsync(long orderId, CreateOrderItemDto itemInput, CancellationToken ct = default);
}

public interface ICustomerService
{
    Task<CustomerDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CreateCustomerDto input, CancellationToken ct = default);
}
