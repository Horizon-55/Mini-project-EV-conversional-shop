namespace EVShop.Ordering.Domain.Entities;

public class OrderDetails
{
    public long OrderId { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string TargetVehicleModel { get; set; } = string.Empty;
    public string ConversionType { get; set; } = "DIY_Kit";
    public string? InstallationNotes { get; set; }
}
