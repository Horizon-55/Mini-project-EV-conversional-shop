using AutoMapper;
using EVShop.Ordering.Bll.DTOs;
using EVShop.Ordering.Domain.Entities;

namespace EVShop.Ordering.Bll.Mapping;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<OrderItem, OrderItemDto>();

        CreateMap<Order, OrderDto>()
            .ForCtorParam("ShippingAddress", opt => opt.MapFrom(src => src.Details != null ? src.Details.ShippingAddress : string.Empty))
            .ForCtorParam("ContactPhone", opt => opt.MapFrom(src => src.Details != null ? src.Details.ContactPhone : string.Empty))
            .ForCtorParam("TargetVehicleModel", opt => opt.MapFrom(src => src.Details != null ? src.Details.TargetVehicleModel : string.Empty))
            .ForCtorParam("ConversionType", opt => opt.MapFrom(src => src.Details != null ? src.Details.ConversionType : "DIY_Kit"))
            .ForCtorParam("InstallationNotes", opt => opt.MapFrom(src => src.Details != null ? src.Details.InstallationNotes : null))
            .ForCtorParam("Items", opt => opt.MapFrom(src => src.Items));

        CreateMap<Customer, CustomerDto>();
    }
}
