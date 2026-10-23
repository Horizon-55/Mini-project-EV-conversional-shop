namespace EVShop.Catalog.Domain.Entities;

public class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Website { get; set; }
    public bool IsActive { get; set; } = true;

    public List<Product> Products { get; } = new();
}
