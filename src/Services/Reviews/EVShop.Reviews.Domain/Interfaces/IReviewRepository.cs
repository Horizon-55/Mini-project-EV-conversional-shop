using EVShop.Reviews.Domain.Entities;
using MongoDB.Bson;

namespace EVShop.Reviews.Domain.Interfaces;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct = default);
    Task<bool> ExistsAsync(long productId, string authorId, CancellationToken ct = default);
    Task AddAsync(Review review, CancellationToken ct = default);
    Task UpdateAsync(Review review, CancellationToken ct = default);
    Task DeleteAsync(ObjectId id, CancellationToken ct = default);
}
