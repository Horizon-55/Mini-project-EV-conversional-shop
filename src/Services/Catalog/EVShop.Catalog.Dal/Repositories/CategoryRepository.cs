using EVShop.Catalog.Dal.Context;
using EVShop.Catalog.Dal.Interfaces;
using EVShop.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EVShop.Catalog.Dal.Repositories;

public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(CatalogDbContext context) : base(context)
    {
    }

    // 2. Explicit Loading — товари категорії довантажуються окремим 2-м запитом (Вимога 2.2 методички)
    public async Task<Category?> GetWithProductsAsync(int id, CancellationToken ct = default)
    {
        // 1-й SQL-запит: отримання самої категорії
        var category = await Set.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null)
            return null;

        // 2-й SQL-запит: явне довантаження залежної колекції лише тоді, коли потрібно
        await Context.Entry(category)
            .Collection(c => c.ProductCategories)
            .Query()
            .Include(pc => pc.Product)
            .LoadAsync(ct);

        return category;
    }
}
