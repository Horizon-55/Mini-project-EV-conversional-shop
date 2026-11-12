using EVShop.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVShop.Catalog.Dal.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedOnAdd();

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(b => b.Name)
            .IsUnique();

        builder.Property(b => b.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.Website)
            .HasMaxLength(250);

        builder.Property(b => b.IsActive)
            .HasDefaultValue(true);

        // Статичний сідінг довідників (Вимога 4.1)
        builder.HasData(
            new Brand { Id = 1, Name = "NetGain Motors", Country = "USA", Website = "https://www.go-ev.com", IsActive = true },
            new Brand { Id = 2, Name = "Curtis Instruments", Country = "USA", Website = "https://www.curtisinstruments.com", IsActive = true },
            new Brand { Id = 3, Name = "Tesla", Country = "USA", Website = "https://www.tesla.com", IsActive = true },
            new Brand { Id = 4, Name = "Nissan", Country = "Japan", Website = "https://www.nissan.com", IsActive = true },
            new Brand { Id = 5, Name = "Orion BMS", Country = "USA", Website = "https://www.orionbms.com", IsActive = true }
        );
    }
}
