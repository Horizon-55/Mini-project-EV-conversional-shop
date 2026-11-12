using EVShop.Catalog.Bll.DTOs;

namespace EVShop.Catalog.Bll.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetListAsync(ProductQuery query, CancellationToken ct = default);
    Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductDto input, CancellationToken ct = default);
    Task<ProductDto> UpdatePriceAsync(int id, decimal newPrice, CancellationToken ct = default);
    Task<ProductDto> AddStockAsync(int id, int quantity, CancellationToken ct = default);
    Task<ProductDto> RemoveStockAsync(int id, int quantity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
