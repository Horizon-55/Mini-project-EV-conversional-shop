using EVShop.Reviews.Domain.Entities;
using EVShop.Reviews.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace EVShop.Reviews.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly MongoDbOptions _options;

    public MongoDbContext(IOptions<MongoDbOptions> options, IMongoClient mongoClient)
    {
        _options = options.Value;
        _database = mongoClient.GetDatabase(_options.DatabaseName);
    }

    public IMongoDatabase Database => _database;

    public IMongoCollection<Review> Reviews =>
        _database.GetCollection<Review>(_options.ReviewsCollectionName);
}
