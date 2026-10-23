using EVShop.Catalog.Domain.Exceptions;

namespace EVShop.Catalog.Domain.Entities;

public class Product
{
    public int Id { get; private set; }
    public int? BrandId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int AvailableStock { get; private set; }
    public bool IsAvailable { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    // Navigation properties
    public Brand? Brand { get; private set; }
    public ProductDetail? Detail { get; private set; } // 1:1
    public List<ProductImage> Images { get; } = new(); // 1:N
    public List<ProductCategory> ProductCategories { get; } = new(); // M:N

    // Приватний конструктор для EF Core
    private Product() { }

    // Конструктор зі створенням валідної сутності
    public Product(string name, string sku, decimal price, int? brandId = null, int availableStock = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name cannot be empty");

        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("SKU cannot be empty");

        if (price <= 0)
            throw new DomainException("Price must be positive");

        if (availableStock < 0)
            throw new DomainException("Available stock cannot be negative");

        Name = name.Trim();
        Sku = sku.Trim().ToUpperInvariant();
        Price = price;
        BrandId = brandId;
        AvailableStock = availableStock;
        IsAvailable = availableStock > 0;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");

        AvailableStock += quantity;
        IsAvailable = AvailableStock > 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");

        if (AvailableStock < quantity)
            throw new DomainException($"Only {AvailableStock} units available");

        AvailableStock -= quantity;
        IsAvailable = AvailableStock > 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new DomainException("Price must be positive");

        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateInfo(string name, int? brandId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name cannot be empty");

        Name = name.Trim();
        BrandId = brandId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDetail(ProductDetail detail)
    {
        Detail = detail ?? throw new ArgumentNullException(nameof(detail));
        detail.ProductId = Id;
    }

    public void AddImage(string url, int sortOrder = 0, bool isMain = false)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("Image URL cannot be empty");

        Images.Add(new ProductImage
        {
            ProductId = Id,
            Url = url.Trim(),
            SortOrder = sortOrder,
            IsMain = isMain
        });
    }

    public void AddCategory(Category category, bool isPrimary = false)
    {
        ArgumentNullException.ThrowIfNull(category);

        if (ProductCategories.Any(pc => pc.CategoryId == category.Id))
            return;

        ProductCategories.Add(new ProductCategory
        {
            ProductId = Id,
            Product = this,
            CategoryId = category.Id,
            Category = category,
            AddedAt = DateTime.UtcNow,
            IsPrimary = isPrimary
        });
    }
}
