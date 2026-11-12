using EVShop.Ordering.Domain.Entities;

namespace EVShop.Ordering.Dal.Interfaces;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<Order?> GetWithItemsAsync(long orderId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(long customerId, CancellationToken ct = default);
    Task ConfirmAsync(long orderId, CancellationToken ct = default); // виклик dbo.usp_ConfirmOrder
    Task PayAsync(long orderId, CancellationToken ct = default);     // виклик dbo.usp_PayOrder
    Task ShipAsync(long orderId, CancellationToken ct = default);    // виклик dbo.usp_ShipOrder
    Task CancelAsync(long orderId, string reason = "Скасовано клієнтом", CancellationToken ct = default);
    Task AddItemAsync(long orderId, long productId, int quantity, decimal discount = 0, CancellationToken ct = default);
}
