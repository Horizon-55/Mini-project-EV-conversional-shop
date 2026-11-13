using EVShop.Reviews.Domain.Common;
using EVShop.Reviews.Domain.Entities;
using EVShop.Reviews.Domain.ValueObjects;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace EVShop.Reviews.Infrastructure.Persistence;

public static class MongoMappings
{
    private static bool _isConfigured;
    private static readonly object _lock = new();

    public static void Configure()
    {
        if (_isConfigured) return;

        lock (_lock)
        {
            if (_isConfigured) return;

            var pack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register("CustomConventions", pack, _ => true);

            if (!BsonClassMap.IsClassMapRegistered(typeof(BaseEntity)))
            {
                BsonClassMap.RegisterClassMap<BaseEntity>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdProperty(c => c.Id);
                    cm.SetIsRootClass(true);
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(Author)))
            {
                BsonClassMap.RegisterClassMap<Author>(cm =>
                {
                    cm.AutoMap();
                    cm.MapCreator(a => new Author(a.UserId, a.DisplayName, a.IsVerifiedBuyer));
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(Comment)))
            {
                BsonClassMap.RegisterClassMap<Comment>(cm =>
                {
                    cm.AutoMap();
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(Review)))
            {
                BsonClassMap.RegisterClassMap<Review>(cm =>
                {
                    cm.AutoMap();
                    cm.MapField("_comments").SetElementName("comments");
                    cm.UnmapProperty(r => r.Comments);
                });
            }

            _isConfigured = true;
        }
    }
}
