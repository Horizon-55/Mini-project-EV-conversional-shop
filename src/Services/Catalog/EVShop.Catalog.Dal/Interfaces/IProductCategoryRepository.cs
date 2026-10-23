using EVShop.Catalog.Domain.Entities;

namespace EVShop.Catalog.Dal.Interfaces;

public interface IProductCategoryRepository : IGenericRepository<ProductCategory>
{
    // M:N через проміжну сутність і її власне поле AddedAt (Вимога 2.2 методички)
    Task<IReadOnlyList<Product>> GetRecentlyAddedAsync(string categoryName, DateTime since, CancellationToken ct = default);
}
