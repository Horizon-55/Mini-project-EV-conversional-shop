using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using EVShop.Catalog.Dal.Context;
using EVShop.Catalog.Dal.Interfaces;
using EVShop.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EVShop.Catalog.Dal.Repositories;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(CatalogDbContext context) : base(context)
    {
    }

    // 1. Eager Loading — пов’язані дані завантажуються одним запитом разом з основною сутністю (Вимога 2.2 методички)
    public async Task<Product?> GetWithDetailsAsync(int id, CancellationToken ct = default)
    {
        return await Set
            .Include(p => p.Detail)
            .Include(p => p.Images)
            .Include(p => p.Brand)
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default)
    {
        return await Set.AnyAsync(p => p.Sku == sku, ct);
    }

    // Використання патерну Specification (Вимога 4.6 методички)
    public async Task<IReadOnlyList<Product>> ListAsync(ISpecification<Product> spec, CancellationToken ct = default)
    {
        var query = SpecificationEvaluator.Default.GetQuery(Set.AsQueryable(), spec);
        return await query.ToListAsync(ct);
    }

    public async Task<int> CountAsync(ISpecification<Product> spec, CancellationToken ct = default)
    {
        var query = SpecificationEvaluator.Default.GetQuery(Set.AsQueryable(), spec);
        return await query.CountAsync(ct);
    }
}
