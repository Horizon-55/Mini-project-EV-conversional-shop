using AutoMapper;
using EVShop.Catalog.Bll.DTOs;
using EVShop.Catalog.Bll.Interfaces;
using EVShop.Catalog.Bll.Specifications;
using EVShop.Catalog.Dal.Interfaces;
using EVShop.Catalog.Domain.Entities;
using EVShop.Catalog.Domain.Exceptions;

namespace EVShop.Catalog.Bll.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public ProductService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    // Посторінкова вибірка з фільтрацією та сортуванням через патерн Specification (Вимога 4.2 та 4.6)
    public async Task<PagedResult<ProductDto>> GetListAsync(ProductQuery query, CancellationToken ct = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var countSpec = new ProductsCountSpec(query);
        var totalCount = await _uow.Products.CountAsync(countSpec, ct);

        var filterSpec = new ProductsFilterSpec(query, pageSize);
        var items = await _uow.Products.ListAsync(filterSpec, ct);

        var dtos = _mapper.Map<List<ProductDto>>(items);
        return new PagedResult<ProductDto>(dtos, totalCount, query.Page, pageSize);
    }

    // Отримання товару за ID з використанням Eager Loading (Вимога 2.2)
    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var product = await _uow.Products.GetWithDetailsAsync(id, ct);
        return product is null ? null : _mapper.Map<ProductDto>(product);
    }

    // Створення товару з перевіркою унікальності SKU (Вимога Частина 3)
    public async Task<ProductDto> CreateAsync(CreateProductDto input, CancellationToken ct = default)
    {
        // Бізнес-перевірка унікальності SKU
        if (await _uow.Products.ExistsBySkuAsync(input.Sku, ct))
            throw new ConflictException($"Товар з артикулом (SKU) '{input.Sku}' уже існує");

        var product = new Product(
            name: input.Name,
            sku: input.Sku,
            price: input.Price,
            brandId: input.BrandId,
            availableStock: input.AvailableStock
        );

        if (!string.IsNullOrWhiteSpace(input.Description) || input.OperatingVoltageV.HasValue)
        {
            product.SetDetail(new ProductDetail
            {
                Description = input.Description ?? string.Empty,
                OperatingVoltageV = input.OperatingVoltageV ?? 100,
                PeakPowerKW = input.PeakPowerKW,
                ContinuousPowerKW = input.ContinuousPowerKW,
                WeightKg = input.WeightKg
            });
        }

        if (!string.IsNullOrWhiteSpace(input.ImageUrl))
        {
            product.AddImage(input.ImageUrl, sortOrder: 1, isMain: true);
        }

        if (input.CategoryId.HasValue)
        {
            var category = await _uow.Categories.GetByIdAsync(input.CategoryId.Value, ct);
            if (category != null)
            {
                product.AddCategory(category, isPrimary: true);
            }
        }

        await _uow.Products.AddAsync(product, ct);
        await _uow.SaveChangesAsync(ct);

        // Повертаємо створений товар з підвантаженими зв'язками
        var created = await _uow.Products.GetWithDetailsAsync(product.Id, ct);
        return _mapper.Map<ProductDto>(created ?? product);
    }

    public async Task<ProductDto> UpdatePriceAsync(int id, decimal newPrice, CancellationToken ct = default)
    {
        var product = await _uow.Products.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException($"Товар з ID {id} не знайдено");

        product.UpdatePrice(newPrice);
        await _uow.SaveChangesAsync(ct);

        var updated = await _uow.Products.GetWithDetailsAsync(product.Id, ct);
        return _mapper.Map<ProductDto>(updated ?? product);
    }

    public async Task<ProductDto> AddStockAsync(int id, int quantity, CancellationToken ct = default)
    {
        var product = await _uow.Products.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException($"Товар з ID {id} не знайдено");

        product.AddStock(quantity);
        await _uow.SaveChangesAsync(ct);

        var updated = await _uow.Products.GetWithDetailsAsync(product.Id, ct);
        return _mapper.Map<ProductDto>(updated ?? product);
    }

    public async Task<ProductDto> RemoveStockAsync(int id, int quantity, CancellationToken ct = default)
    {
        var product = await _uow.Products.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException($"Товар з ID {id} не знайдено");

        product.RemoveStock(quantity); // Викидає DomainException при нестачі на складі
        await _uow.SaveChangesAsync(ct);

        var updated = await _uow.Products.GetWithDetailsAsync(product.Id, ct);
        return _mapper.Map<ProductDto>(updated ?? product);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var product = await _uow.Products.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException($"Товар з ID {id} не знайдено");

        _uow.Products.Delete(product);
        await _uow.SaveChangesAsync(ct);
    }
}
