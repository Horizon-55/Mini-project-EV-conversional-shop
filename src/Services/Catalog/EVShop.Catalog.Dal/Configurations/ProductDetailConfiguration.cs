using EVShop.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVShop.Catalog.Dal.Configurations;

public class ProductDetailConfiguration : IEntityTypeConfiguration<ProductDetail>
{
    public void Configure(EntityTypeBuilder<ProductDetail> builder)
    {
        builder.ToTable("ProductDetails");

        // PK є водночас FK (зв'язок 1:1, сторінка 3 методички)
        builder.HasKey(d => d.ProductId);
        builder.Property(d => d.ProductId).ValueGeneratedNever();

        builder.Property(d => d.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(d => d.Manufacturer)
            .HasMaxLength(100);

        builder.Property(d => d.OperatingVoltageV)
            .IsRequired();

        builder.Property(d => d.PeakPowerKW)
            .HasColumnType("decimal(6,2)");

        builder.Property(d => d.ContinuousPowerKW)
            .HasColumnType("decimal(6,2)");

        builder.Property(d => d.WeightKg)
            .HasColumnType("decimal(6,2)");
    }
}
