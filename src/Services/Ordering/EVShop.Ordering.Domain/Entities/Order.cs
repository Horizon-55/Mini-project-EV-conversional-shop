namespace EVShop.Ordering.Domain.Entities;

public class Order
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string Status { get; set; } = "Submitted";
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "customer";
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public byte[]? RowVersion { get; set; }

    // Зв'язок 1:1
    public OrderDetails? Details { get; set; }

    // Зв'язок 1:N до позицій (OrderItems)
    public List<OrderItem> Items { get; set; } = new();
}
