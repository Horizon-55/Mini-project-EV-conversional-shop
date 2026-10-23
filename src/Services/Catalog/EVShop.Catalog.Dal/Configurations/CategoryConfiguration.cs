using EVShop.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVShop.Catalog.Dal.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.Slug)
            .IsUnique();

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        // Статичний сідінг довідника категорій (Вимога 4.1 методички)
        builder.HasData(
            new Category { Id = 1, Name = "Electric Motors", Slug = "electric-motors", Description = "AC/DC тягові електродвигуни для конверсії", DisplayOrder = 1, IsActive = true },
            new Category { Id = 2, Name = "Motor Controllers & Inverters", Slug = "controllers-inverters", Description = "Інвертори напруги та силові контролери керування двигуном", DisplayOrder = 2, IsActive = true },
            new Category { Id = 3, Name = "Traction Batteries", Slug = "traction-batteries", Description = "Тягові літієві акумуляторні блоки та модулі", DisplayOrder = 3, IsActive = true },
            new Category { Id = 4, Name = "BMS & Battery Accessories", Slug = "bms-accessories", Description = "Системи контролю заряду/розряду батарей (BMS)", DisplayOrder = 4, IsActive = true },
            new Category { Id = 5, Name = "Charging Equipment", Slug = "chargers", Description = "Бортові зарядні пристрої Type 2 / CCS2", DisplayOrder = 5, IsActive = true }
        );
    }
}
