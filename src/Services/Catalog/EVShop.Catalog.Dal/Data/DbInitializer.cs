using EVShop.Catalog.Dal.Context;
using EVShop.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EVShop.Catalog.Dal.Data;

// Програмний сідінг тестових даних після виконання міграцій (Вимога 4.1 методички)
public static class DbInitializer
{
    public static async Task SeedAsync(CatalogDbContext db, CancellationToken ct = default)
    {
        // Ідемпотентна перевірка: якщо товари вже є, сідінг пропускається (не створює дублікатів)
        if (await db.Products.AnyAsync(ct))
            return;

        // 1. Двигун Hyper9 (NetGain Motors)
        var hyper9 = new Product(
            name: "Hyper9 AC Synchronous Motor Kit",
            sku: "MOT-HYPER9-100V",
            price: 4850.00m,
            brandId: 1, // NetGain Motors
            availableStock: 12
        );
        hyper9.SetDetail(new ProductDetail
        {
            Description = "Високоефективний синхронний тяговий двигун на постійних магнітах (SRIPM) з контролером Curtis для легкових електроконверсій.",
            Manufacturer = "NetGain Motors, Inc.",
            OperatingVoltageV = 100,
            PeakPowerKW = 100.00m,
            ContinuousPowerKW = 38.00m,
            WeightKg = 54.50m
        });

        hyper9.AddImage("https://evshop.local/images/hyper9_kit_main.jpg", sortOrder: 1, isMain: true);

        // 2. Контролер Curtis 1238SE
        var curtisController = new Product(
            name: "Curtis 1238SE-7971 AC Controller",
            sku: "CTRL-CURTIS-1238SE",
            price: 2450.00m,
            brandId: 2, // Curtis Instruments
            availableStock: 8
        );
        curtisController.SetDetail(new ProductDetail
        {
            Description = "Векторний інвертор-контролер 96-120V / 650A з водяним охолодженням для електродвигунів змінного струму.",
            Manufacturer = "Curtis Instruments",
            OperatingVoltageV = 120,
            PeakPowerKW = 78.00m,
            ContinuousPowerKW = 45.00m,
            WeightKg = 6.80m
        });
        curtisController.AddImage("https://evshop.local/images/curtis_1238se.jpg", sortOrder: 1, isMain: true);

        // 3. Тяговий батарейний модуль Tesla 5.3 kWh
        var teslaModule = new Product(
            name: "Tesla Model S 5.3 kWh Battery Module",
            sku: "BAT-TESLA-MOD-S",
            price: 1350.00m,
            brandId: 3, // Tesla
            availableStock: 24
        );
        teslaModule.SetDetail(new ProductDetail
        {
            Description = "Оригінальний модуль 24V / 250Ah (конфігурація 6S74P на елементах Panasonic 18650) з вбудованим рідинним контуром охолодження.",
            Manufacturer = "Tesla Motors",
            OperatingVoltageV = 24,
            PeakPowerKW = 35.00m,
            ContinuousPowerKW = 15.00m,
            WeightKg = 25.00m
        });
        teslaModule.AddImage("https://evshop.local/images/tesla_module_53kwh.jpg", sortOrder: 1, isMain: true);

        // 4. Система керування батареєю Orion BMS 2
        var orionBms = new Product(
            name: "Orion BMS 2 (48-cell Extended)",
            sku: "BMS-ORION-48C",
            price: 890.00m,
            brandId: 5, // Orion BMS
            availableStock: 15
        );
        orionBms.SetDetail(new ProductDetail
        {
            Description = "Повнофункціональна система управління батареєю (до 48 послідовних осередків) з CAN-шиною та термодатчиками.",
            Manufacturer = "Ewert Energy Systems",
            OperatingVoltageV = 180,
            PeakPowerKW = null,
            ContinuousPowerKW = null,
            WeightKg = 1.20m
        });
        orionBms.AddImage("https://evshop.local/images/orion_bms_2.jpg", sortOrder: 1, isMain: true);

        // Додаємо товари в контекст
        await db.Products.AddRangeAsync(hyper9, curtisController, teslaModule, orionBms);
        await db.SaveChangesAsync(ct);

        // Прив'язка до категорій (M:N зв'язок)
        var categoryMotors = await db.Categories.FindAsync(new object[] { 1 }, ct);
        var categoryControllers = await db.Categories.FindAsync(new object[] { 2 }, ct);
        var categoryBatteries = await db.Categories.FindAsync(new object[] { 3 }, ct);
        var categoryBms = await db.Categories.FindAsync(new object[] { 4 }, ct);

        if (categoryMotors != null)
            hyper9.AddCategory(categoryMotors, isPrimary: true);

        if (categoryControllers != null)
        {
            hyper9.AddCategory(categoryControllers, isPrimary: false);
            curtisController.AddCategory(categoryControllers, isPrimary: true);
        }

        if (categoryBatteries != null)
            teslaModule.AddCategory(categoryBatteries, isPrimary: true);

        if (categoryBms != null)
            orionBms.AddCategory(categoryBms, isPrimary: true);

        await db.SaveChangesAsync(ct);
    }
}
