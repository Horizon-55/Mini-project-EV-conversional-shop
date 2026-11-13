using AutoMapper;
using EVShop.Reviews.Application.DTOs;
using EVShop.Reviews.Domain.Entities;
using EVShop.Reviews.Domain.Interfaces;
using EVShop.Reviews.Domain.ValueObjects;
using MediatR;
using MongoDB.Bson;

namespace EVShop.Reviews.Application.Reviews.Commands.CreateReview;

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, ReviewDto>
{
    private readonly IReviewRepository _repository;
    private readonly IMapper _mapper;

    public CreateReviewCommandHandler(IReviewRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ReviewDto> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var author = new Author(
            request.Author.UserId,
            request.Author.DisplayName,
            request.Author.IsVerifiedBuyer
        );

        ObjectId? discussionId = string.IsNullOrEmpty(request.DiscussionId)
            ? null
            : ObjectId.Parse(request.DiscussionId);

        var review = new Review(
            request.ProductId,
            request.ProductName,
            author,
            request.Rating,
            request.Text,
            discussionId
        );

        await _repository.AddAsync(review, cancellationToken);

        return _mapper.Map<ReviewDto>(review);
    }
}
