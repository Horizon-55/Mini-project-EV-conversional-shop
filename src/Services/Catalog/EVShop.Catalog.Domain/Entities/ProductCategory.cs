namespace EVShop.Catalog.Domain.Entities;

public class ProductCategory
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public bool IsPrimary { get; set; }
}
