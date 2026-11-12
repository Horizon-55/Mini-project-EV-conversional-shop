namespace EVShop.Ordering.Bll.DTOs;

public record OrderDto(
    long Id,
    long CustomerId,
    string Status,
    DateTime OrderDate,
    decimal TotalAmount,
    string ShippingAddress,
    string ContactPhone,
    string TargetVehicleModel,
    string ConversionType,
    string? InstallationNotes,
    IReadOnlyList<OrderItemDto> Items
);
