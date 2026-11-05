using EVShop.Reviews.Application.Common.Interfaces;
using EVShop.Reviews.Application.Common.Models;
using EVShop.Reviews.Application.DTOs;

namespace EVShop.Reviews.Application.Reviews.Queries.GetReviewsList;

public record GetReviewsListQuery(
    long? ProductId = null,
    int? Rating = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 10
) : IQuery<PagedResult<ReviewDto>>;
