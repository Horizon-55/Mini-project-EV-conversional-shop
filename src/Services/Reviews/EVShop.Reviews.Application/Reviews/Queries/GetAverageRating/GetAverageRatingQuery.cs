using EVShop.Reviews.Application.Common.Interfaces;

namespace EVShop.Reviews.Application.Reviews.Queries.GetAverageRating;

public record GetAverageRatingQuery(long ProductId) : IQuery<double>;
