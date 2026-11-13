using EVShop.Reviews.Application.Common.Interfaces;
using EVShop.Reviews.Application.DTOs;

namespace EVShop.Reviews.Application.Reviews.Queries.GetReviewById;

public record GetReviewByIdQuery(string Id) : IQuery<ReviewDto>;
