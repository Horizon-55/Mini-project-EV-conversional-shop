using EVShop.Ordering.Domain.Entities;

namespace EVShop.Ordering.Dal.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    Task DecreaseStockAsync(long productId, int quantity, CancellationToken ct = default);
    Task IncreaseStockAsync(long productId, int quantity, CancellationToken ct = default);
}
