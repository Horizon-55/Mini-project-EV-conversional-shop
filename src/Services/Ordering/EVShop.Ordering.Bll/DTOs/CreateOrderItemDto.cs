using System.ComponentModel.DataAnnotations;

namespace EVShop.Ordering.Bll.DTOs;

public class CreateOrderItemDto
{
    [Range(1, long.MaxValue, ErrorMessage = "ProductId повинен бути додатнім числом.")]
    public long ProductId { get; set; }

    [Range(1, 1000, ErrorMessage = "Кількість повинна бути в діапазоні від 1 до 1000.")]
    public int Quantity { get; set; } = 1;

    // Псевдонім для сумісності з методичкою
    public int Units
    {
        get => Quantity;
        set => Quantity = value;
    }

    [Range(0, 100000, ErrorMessage = "Знижка не може бути від'ємною.")]
    public decimal Discount { get; set; } = 0;
}
