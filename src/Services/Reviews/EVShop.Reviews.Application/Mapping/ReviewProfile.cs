using AutoMapper;
using EVShop.Reviews.Application.DTOs;
using EVShop.Reviews.Domain.Entities;
using EVShop.Reviews.Domain.ValueObjects;

namespace EVShop.Reviews.Application.Mapping;

public class ReviewProfile : Profile
{
    public ReviewProfile()
    {
        CreateMap<Author, AuthorDto>();
        CreateMap<AuthorDto, Author>();

        CreateMap<Comment, CommentDto>();

        CreateMap<Review, ReviewDto>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.DiscussionId, opt => opt.MapFrom(s => s.DiscussionId.HasValue ? s.DiscussionId.Value.ToString() : null))
            .ForMember(d => d.Comments, opt => opt.MapFrom(s => s.Comments));
    }
}
