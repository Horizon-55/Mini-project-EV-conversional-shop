using AutoMapper;
using EVShop.Catalog.Bll.DTOs;
using EVShop.Catalog.Bll.Interfaces;
using EVShop.Catalog.Dal.Interfaces;

namespace EVShop.Catalog.Bll.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public CategoryService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var categories = await _uow.Categories.GetAllAsync(ct);
        return _mapper.Map<IReadOnlyList<CategoryDto>>(categories);
    }

    // Демонстрація Explicit Loading: категорія довантажує свої товари окремим другим запитом (Вимога 2.2)
    public async Task<CategoryDto?> GetWithProductsAsync(int id, CancellationToken ct = default)
    {
        var category = await _uow.Categories.GetWithProductsAsync(id, ct);
        return category is null ? null : _mapper.Map<CategoryDto>(category);
    }

    // Демонстрація LINQ to Entities: вибірка нещодавно доданих товарів за проміжною таблицею M:N (Вимога 2.2)
    public async Task<IReadOnlyList<ProductDto>> GetRecentlyAddedAsync(
        string categoryName, DateTime since, CancellationToken ct = default)
    {
        var products = await _uow.ProductCategories.GetRecentlyAddedAsync(categoryName, since, ct);
        return _mapper.Map<IReadOnlyList<ProductDto>>(products);
    }
}
