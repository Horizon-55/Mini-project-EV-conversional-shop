using EVShop.Catalog.Domain.Entities;

namespace EVShop.Catalog.Dal.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IProductRepository Products { get; }
    ICategoryRepository Categories { get; }
    IProductCategoryRepository ProductCategories { get; }
    IGenericRepository<Brand> Brands { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
