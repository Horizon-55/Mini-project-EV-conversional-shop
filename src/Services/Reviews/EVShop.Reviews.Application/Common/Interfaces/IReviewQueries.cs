using EVShop.Reviews.Application.Common.Models;
using EVShop.Reviews.Application.DTOs;

namespace EVShop.Reviews.Application.Common.Interfaces;

public interface IReviewQueries
{
    Task<ReviewDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<PagedResult<ReviewDto>> GetListAsync(long? productId, int? rating, string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<double> GetAverageRatingAsync(long productId, CancellationToken cancellationToken = default);
}
