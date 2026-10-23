using AutoMapper;
using EVShop.Catalog.Bll.DTOs;
using EVShop.Catalog.Domain.Entities;

namespace EVShop.Catalog.Bll.Mapping;

public class CatalogProfile : Profile
{
    public CatalogProfile()
    {
        CreateMap<Product, ProductDto>()
            .ForCtorParam(nameof(ProductDto.BrandName),
                opt => opt.MapFrom(src => src.Brand != null ? src.Brand.Name : null))
            .ForCtorParam(nameof(ProductDto.Categories),
                opt => opt.MapFrom(src => src.ProductCategories
                    .Where(pc => pc.Category != null)
                    .Select(pc => pc.Category.Name)
                    .ToList()))
            .ForCtorParam(nameof(ProductDto.Description),
                opt => opt.MapFrom(src => src.Detail != null ? src.Detail.Description : null))
            .ForCtorParam(nameof(ProductDto.MainImageUrl),
                opt => opt.MapFrom(src => src.Images.FirstOrDefault(i => i.IsMain) != null
                    ? src.Images.First(i => i.IsMain).Url
                    : src.Images.Select(i => i.Url).FirstOrDefault()));

        CreateMap<Category, CategoryDto>()
            .ForCtorParam(nameof(CategoryDto.Products),
                opt => opt.MapFrom(src => src.ProductCategories
                    .Where(pc => pc.Product != null)
                    .Select(pc => pc.Product)
                    .ToList()));
    }
}
