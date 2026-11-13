using EVShop.Reviews.Application.Common.Interfaces;
using EVShop.Reviews.Application.DTOs;
using EVShop.Reviews.Domain.Exceptions;
using MediatR;

namespace EVShop.Reviews.Application.Reviews.Queries.GetReviewById;

public class GetReviewByIdQueryHandler : IRequestHandler<GetReviewByIdQuery, ReviewDto>
{
    private readonly IReviewQueries _queries;

    public GetReviewByIdQueryHandler(IReviewQueries queries)
    {
        _queries = queries;
    }

    public async Task<ReviewDto> Handle(GetReviewByIdQuery request, CancellationToken cancellationToken)
    {
        var review = await _queries.GetByIdAsync(request.Id, cancellationToken);

        if (review == null)
            throw new NotFoundException("Review", request.Id);

        return review;
    }
}
