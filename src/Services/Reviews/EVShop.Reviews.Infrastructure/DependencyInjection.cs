using EVShop.Reviews.Application.Common.Interfaces;
using EVShop.Reviews.Domain.Interfaces;
using EVShop.Reviews.Infrastructure.Configuration;
using EVShop.Reviews.Infrastructure.Persistence;
using EVShop.Reviews.Infrastructure.Seeding;
using EVShop.Reviews.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace EVShop.Reviews.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Configure MongoDB Bson mappings & conventions
        MongoMappings.Configure();

        // 2. Options binding
        services.Configure<MongoDbOptions>(configuration.GetSection(MongoDbOptions.SectionName));

        // 3. Mongo Client & DbContext
        services.AddSingleton<IMongoClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            return new MongoClient(options.ConnectionString);
        });

        services.AddScoped<MongoDbContext>();

        // 4. Persistence & CQRS read/write separation
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReviewQueries, ReviewQueries>();

        // 5. Seeding & Background Index Maintenance
        services.AddScoped<IDataSeeder, ReviewSeeder>();
        services.AddHostedService<IndexCreationService>();

        return services;
    }
}
