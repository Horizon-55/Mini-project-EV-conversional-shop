using System.ComponentModel.DataAnnotations;

namespace EVShop.Catalog.Bll.DTOs;

public record ProductDto(
    int Id,
    string Name,
    string Sku,
    decimal Price,
    int AvailableStock,
    bool IsAvailable,
    int? BrandId,
    string? BrandName,
    IReadOnlyList<string> Categories,
    string? Description,
    string? MainImageUrl,
    DateTime CreatedAt
);

public record CreateProductDto
{
    [Required(ErrorMessage = "Назва товару є обов'язковою")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Назва має містити від 2 до 200 символів")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "SKU є обов'язковим")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "SKU має містити від 3 до 50 символів")]
    public string Sku { get; init; } = string.Empty;

    [Range(0.01, 1000000.00, ErrorMessage = "Ціна повинна бути більшою за 0")]
    public decimal Price { get; init; }

    [Range(0, 100000, ErrorMessage = "Залишок не може бути від'ємним")]
    public int AvailableStock { get; init; }

    public int? BrandId { get; init; }
    public int? CategoryId { get; init; }

    [StringLength(2000, ErrorMessage = "Опис не повинен перевищувати 2000 символів")]
    public string? Description { get; init; }

    [Range(12, 1000, ErrorMessage = "Робоча напруга має бути в межах 12-1000 В")]
    public int? OperatingVoltageV { get; init; }
    public decimal? PeakPowerKW { get; init; }
    public decimal? ContinuousPowerKW { get; init; }
    public decimal? WeightKg { get; init; }
    public string? ImageUrl { get; init; }
}

public record UpdateProductPriceDto(
    [Range(0.01, 1000000.00, ErrorMessage = "Нова ціна повинна бути більшою за 0")]
    decimal NewPrice
);

public record AdjustStockDto(
    [Range(1, 10000, ErrorMessage = "Кількість повинна бути більшою за 0")]
    int Quantity
);

public record CategoryDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<ProductDto>? Products = null
);
