using Ardalis.Specification;
using EVShop.Catalog.Domain.Entities;

namespace EVShop.Catalog.Dal.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    // Eager Loading: картка товару з усім пов’язаним одним запитом (Вимога 2.2 методички)
    Task<Product?> GetWithDetailsAsync(int id, CancellationToken ct = default);

    Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default);

    // Патерн Specification (Вимога 4.6 методички)
    Task<IReadOnlyList<Product>> ListAsync(ISpecification<Product> spec, CancellationToken ct = default);
    Task<int> CountAsync(ISpecification<Product> spec, CancellationToken ct = default);
}
