using AutoMapper;
using EVShop.Reviews.Application.Common.Interfaces;
using EVShop.Reviews.Application.Common.Models;
using EVShop.Reviews.Application.DTOs;
using EVShop.Reviews.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EVShop.Reviews.Infrastructure.Persistence;

public class ReviewQueries : IReviewQueries
{
    private readonly MongoDbContext _context;
    private readonly IMapper _mapper;

    public ReviewQueries(MongoDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ReviewDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return null;

        var review = await _context.Reviews
            .Find(r => r.Id == objectId)
            .FirstOrDefaultAsync(cancellationToken);

        return review == null ? null : _mapper.Map<ReviewDto>(review);
    }

    public async Task<PagedResult<ReviewDto>> GetListAsync(
        long? productId,
        int? rating,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<Review>.Filter;
        var filter = filterBuilder.Empty;

        if (productId.HasValue)
        {
            filter &= filterBuilder.Eq(r => r.ProductId, productId.Value);
        }

        if (rating.HasValue)
        {
            filter &= filterBuilder.Eq(r => r.Rating, rating.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            filter &= filterBuilder.Text(searchTerm);
        }

        var totalCount = await _context.Reviews.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var reviews = await _context.Reviews
            .Find(filter)
            .SortByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<IReadOnlyCollection<ReviewDto>>(reviews);

        return new PagedResult<ReviewDto>(dtos, totalCount, page, pageSize);
    }

    public async Task<double> GetAverageRatingAsync(long productId, CancellationToken cancellationToken = default)
    {
        var pipeline = new EmptyPipelineDefinition<Review>()
            .Match(r => r.ProductId == productId)
            .Group(r => 1, g => new { AverageRating = g.Average(x => x.Rating) });

        var result = await _context.Reviews.Aggregate(pipeline, cancellationToken: cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);

        return result == null ? 0.0 : Math.Round(result.AverageRating, 2);
    }
}
