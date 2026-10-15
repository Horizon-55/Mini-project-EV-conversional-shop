using System.ComponentModel.DataAnnotations;

namespace EVShop.Ordering.Bll.DTOs;

public class CreateOrderDto
{
    [Range(1, long.MaxValue, ErrorMessage = "CustomerId повинен бути дійсним ідентифікатором.")]
    public long CustomerId { get; set; }

    [Required(ErrorMessage = "Адреса доставки є обов'язковою.")]
    [StringLength(400, MinimumLength = 5, ErrorMessage = "Адреса має містити від 5 до 400 символів.")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Контактний телефон є обов'язковим.")]
    [Phone(ErrorMessage = "Некоректний формат номера телефону.")]
    public string ContactPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть модель авто під конверсію (наприклад, BMW E30).")]
    public string TargetVehicleModel { get; set; } = string.Empty;

    public string ConversionType { get; set; } = "DIY_Kit";

    public string? InstallationNotes { get; set; }

    [Required(ErrorMessage = "Замовлення повинно містити щонайменше одну позицію товару.")]
    [MinLength(1, ErrorMessage = "Список товарів не може бути порожнім.")]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}
