using AutoMapper;
using EVShop.Reviews.Application.DTOs;
using EVShop.Reviews.Domain.Exceptions;
using EVShop.Reviews.Domain.Interfaces;
using MediatR;
using MongoDB.Bson;

namespace EVShop.Reviews.Application.Reviews.Commands.UpdateReview;

public class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand, ReviewDto>
{
    private readonly IReviewRepository _repository;
    private readonly IMapper _mapper;

    public UpdateReviewCommandHandler(IReviewRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ReviewDto> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        var objectId = ObjectId.Parse(request.Id);
        var review = await _repository.GetByIdAsync(objectId, cancellationToken);

        if (review == null)
            throw new NotFoundException("Review", request.Id);

        if (review.Version != request.ExpectedVersion)
        {
            throw new ConflictException(
                $"Optimistic concurrency violation: review has version {review.Version}, but expected {request.ExpectedVersion}."
            );
        }

        review.Update(request.Rating, request.Text);

        await _repository.UpdateAsync(review, cancellationToken);

        return _mapper.Map<ReviewDto>(review);
    }
}
