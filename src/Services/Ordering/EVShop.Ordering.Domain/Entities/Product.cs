namespace EVShop.Ordering.Domain.Entities;

public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
}
