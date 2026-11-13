using EVShop.Reviews.Domain.Entities;
using EVShop.Reviews.Domain.Exceptions;
using EVShop.Reviews.Domain.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EVShop.Reviews.Infrastructure.Persistence;

public class ReviewRepository : IReviewRepository
{
    private readonly MongoDbContext _context;

    public ReviewRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
    {
        return await _context.Reviews
            .Find(r => r.Id == id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> ExistsAsync(long productId, string authorId, CancellationToken ct = default)
    {
        return await _context.Reviews
            .Find(r => r.ProductId == productId && r.Author.UserId == authorId)
            .AnyAsync(ct);
    }

    public async Task AddAsync(Review review, CancellationToken ct = default)
    {
        try
        {
            await _context.Reviews.InsertOneAsync(review, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new ConflictException("A review for this product has already been submitted by this user.", ex);
        }
    }

    public async Task UpdateAsync(Review review, CancellationToken ct = default)
    {
        var expectedVersion = review.Version;
        review.BumpVersion();

        var filter = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Eq(r => r.Id, review.Id),
            Builders<Review>.Filter.Eq(r => r.Version, expectedVersion)
        );

        var result = await _context.Reviews.ReplaceOneAsync(filter, review, cancellationToken: ct);

        if (result.MatchedCount == 0)
        {
            throw new ConflictException(
                $"Optimistic concurrency violation: review '{review.Id}' was modified by another transaction (expected version {expectedVersion})."
            );
        }
    }

    public async Task DeleteAsync(ObjectId id, CancellationToken ct = default)
    {
        var result = await _context.Reviews.DeleteOneAsync(r => r.Id == id, ct);
        if (result.DeletedCount == 0)
        {
            throw new NotFoundException("Review", id);
        }
    }
}
