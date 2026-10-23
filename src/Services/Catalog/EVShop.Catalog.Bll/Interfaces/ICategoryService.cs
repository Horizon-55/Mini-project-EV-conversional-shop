using EVShop.Catalog.Bll.DTOs;

namespace EVShop.Catalog.Bll.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryDto?> GetWithProductsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ProductDto>> GetRecentlyAddedAsync(string categoryName, DateTime since, CancellationToken ct = default);
}
