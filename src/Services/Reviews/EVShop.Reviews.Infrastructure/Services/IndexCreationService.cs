using EVShop.Reviews.Domain.Entities;
using EVShop.Reviews.Infrastructure.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EVShop.Reviews.Infrastructure.Services;

public class IndexCreationService : IHostedService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<IndexCreationService> _logger;

    public IndexCreationService(MongoDbContext context, ILogger<IndexCreationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Ensuring MongoDB indexes for 'reviews' collection...");

            var indexKeysDefinition = Builders<Review>.IndexKeys;

            var models = new[]
            {
                // 1. Compound index for querying reviews by product sorted by date
                new CreateIndexModel<Review>(
                    indexKeysDefinition.Ascending(r => r.ProductId).Descending(r => r.CreatedAt),
                    new CreateIndexOptions { Name = "idx_reviews_product_created" }
                ),

                // 2. Unique compound index: a user can only review a product once
                new CreateIndexModel<Review>(
                    indexKeysDefinition.Ascending(r => r.ProductId).Ascending("author.userId"),
                    new CreateIndexOptions { Name = "idx_reviews_unique_user_product", Unique = true }
                ),

                // 3. Full-text search index for searching review content
                new CreateIndexModel<Review>(
                    indexKeysDefinition.Text(r => r.Text),
                    new CreateIndexOptions { Name = "idx_reviews_text_search" }
                )
            };

            await _context.Reviews.Indexes.CreateManyAsync(models, cancellationToken);
            _logger.LogInformation("MongoDB indexes verified successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MongoDB indexes on startup: {Message}", ex.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
