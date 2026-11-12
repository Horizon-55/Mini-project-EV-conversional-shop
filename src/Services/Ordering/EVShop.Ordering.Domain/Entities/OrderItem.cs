namespace EVShop.Ordering.Domain.Entities;

public class OrderItem
{
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal Discount { get; set; }
    public decimal LineTotal => (Quantity * UnitPrice) - Discount;

    // Псевдонім для сумісності з прикладом методички (Units == Quantity)
    public int Units
    {
        get => Quantity;
        set => Quantity = value;
    }
}
