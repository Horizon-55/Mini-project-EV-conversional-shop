namespace EVShop.Catalog.Domain.Entities;

public class ProductDetail
{
    public int ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public int OperatingVoltageV { get; set; }
    public decimal? PeakPowerKW { get; set; }
    public decimal? ContinuousPowerKW { get; set; }
    public decimal? WeightKg { get; set; }

    public Product? Product { get; set; }
}
