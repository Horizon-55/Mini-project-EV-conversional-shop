using EVShop.Catalog.Dal.Context;
using EVShop.Catalog.Dal.Interfaces;
using EVShop.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EVShop.Catalog.Dal.Repositories;

public class ProductCategoryRepository : GenericRepository<ProductCategory>, IProductCategoryRepository
{
    public ProductCategoryRepository(CatalogDbContext context) : base(context)
    {
    }

    // 3. LINQ to Entities — вибірка «багато до багатьох» із використанням проміжної сутності та її поля AddedAt (Вимога 2.2 методички)
    public async Task<IReadOnlyList<Product>> GetRecentlyAddedAsync(
        string categoryName, DateTime since, CancellationToken ct = default)
    {
        return await Set
            .Where(pc => pc.Category.Name == categoryName && pc.AddedAt >= since)
            .OrderByDescending(pc => pc.AddedAt)
            .Select(pc => pc.Product)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
