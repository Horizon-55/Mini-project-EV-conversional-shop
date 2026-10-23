using EVShop.Catalog.Domain.Entities;

namespace EVShop.Catalog.Dal.Interfaces;

public interface ICategoryRepository : IGenericRepository<Category>
{
    // Explicit Loading: товари категорії довантажуються окремим 2-м запитом (Вимога 2.2 методички)
    Task<Category?> GetWithProductsAsync(int id, CancellationToken ct = default);
}
