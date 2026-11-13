using EVShop.Reviews.Application.Common.Interfaces;
using EVShop.Reviews.Application.Common.Models;
using EVShop.Reviews.Application.DTOs;
using MediatR;

namespace EVShop.Reviews.Application.Reviews.Queries.GetReviewsList;

public class GetReviewsListQueryHandler : IRequestHandler<GetReviewsListQuery, PagedResult<ReviewDto>>
{
    private readonly IReviewQueries _queries;

    public GetReviewsListQueryHandler(IReviewQueries queries)
    {
        _queries = queries;
    }

    public async Task<PagedResult<ReviewDto>> Handle(GetReviewsListQuery request, CancellationToken cancellationToken)
    {
        return await _queries.GetListAsync(
            request.ProductId,
            request.Rating,
            request.SearchTerm,
            request.Page,
            request.PageSize,
            cancellationToken
        );
    }
}
