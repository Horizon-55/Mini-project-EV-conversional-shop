using EVShop.Reviews.Domain.Entities;
using EVShop.Reviews.Domain.ValueObjects;
using EVShop.Reviews.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EVShop.Reviews.Infrastructure.Seeding;

public class ReviewSeeder : IDataSeeder
{
    private readonly MongoDbContext _context;
    private readonly ILogger<ReviewSeeder> _logger;

    public ReviewSeeder(MongoDbContext context, ILogger<ReviewSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var count = await _context.Reviews.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);
        if (count > 0)
        {
            _logger.LogInformation("Reviews collection already contains {Count} documents. Skipping seeding.", count);
            return;
        }

        _logger.LogInformation("Seeding initial Reviews data...");

        var r1 = new Review(
            productId: 1,
            productName: "Nissan Leaf 40kWh 2021",
            author: new Author("usr_101", "Олександр Коваленко", isVerifiedBuyer: true),
            rating: 5,
            text: "Чудовий міський електромобіль! Реальний запас ходу влітку близько 240 км. Швидка зарядка CHAdeMO працює стабільно."
        );
        r1.AddComment(new Author("mod_1", "Менеджер EVShop", isVerifiedBuyer: false), "Дякуємо за відгук, Олександре! Нехай авто приносить задоволення.");

        var r2 = new Review(
            productId: 1,
            productName: "Nissan Leaf 40kWh 2021",
            author: new Author("usr_102", "Ірина Мельник", isVerifiedBuyer: true),
            rating: 4,
            text: "Гарний стан батареї SOH 93%, комфортний салон та камери 360 ProPilot. Взимку запас ходу трохи падає, але для міста вистачає з головою."
        );

        var r3 = new Review(
            productId: 2,
            productName: "Tesla Model 3 Long Range 2022",
            author: new Author("usr_103", "Максим Бондар", isVerifiedBuyer: true),
            rating: 5,
            text: "Динаміка розгону вражає, повний привід AWD впевнено тримає дорогу на слизькому покритті. Автопілот суттєво полегшує далекі поїздки."
        );
        r3.AddComment(new Author("usr_104", "Денис", isVerifiedBuyer: false), "Чи встановлений CCS2 ретрофіт?");
        r3.AddComment(new Author("usr_103", "Максим Бондар", isVerifiedBuyer: true), "Так, європейський порт вже з заводу.");

        var r4 = new Review(
            productId: 3,
            productName: "BMW i4 eDrive40 2023",
            author: new Author("usr_105", "Тарас Шевчук", isVerifiedBuyer: true),
            rating: 5,
            text: "Преміальна шумоізоляція та матеріали оздоблення салону. Справжнє задоволення за кермом у спортивному режимі."
        );

        await _context.Reviews.InsertManyAsync(new[] { r1, r2, r3, r4 }, cancellationToken: cancellationToken);
        _logger.LogInformation("Successfully seeded 4 initial reviews.");
    }
}
