namespace EVShop.Ordering.Bll.DTOs;

public record OrderItemDto(
    long ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal Discount,
    decimal LineTotal
)
{
    // Підтримка властивості Units для збігу з прикладом сторінки 9 методички
    public int Units => Quantity;
}
