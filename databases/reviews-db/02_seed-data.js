// Тестові документи для MongoDB (відгуки, обговорення, портфоліо)
// Демонструє:
// 1. Embedding (вбудований автор та коментарі)
// 2. Referencing (productId без FK, discussionId)
// 3. Динамічну схему (різні набори полів для двигунів, батарей, плит)

const dbName = "evshop_reviews";
const db = db.getSiblingDB(dbName);

print("Заповнення бази тестовими документами: " + dbName);

// 1. Теми форуму (discussions)
const disc1Id = new ObjectId("66f0c3000000000000000001");
const disc2Id = new ObjectId("66f0c3000000000000000002");

db.discussions.deleteMany({ _id: { $in: [disc1Id, disc2Id] } });

db.discussions.insertMany([
    {
        _id: disc1Id,
        title: "Підключення інвертора Sevcon Gen4 до педалі газу через CAN-bus",
        topic: "Motor_Inverter",
        starter: {
            userId: "usr-101",
            displayName: "Тарас Електротехнік"
        },
        messageCount: 14,
        createdAt: new Date("2026-08-15T14:30:00Z")
    },
    {
        _id: disc2Id,
        title: "Охолодження батарей CATL 12S при швидкій зарядці 150kW",
        topic: "Battery_BMS",
        starter: {
            userId: "usr-102",
            displayName: "Олексій Термодинаміка"
        },
        messageCount: 8,
        createdAt: new Date("2026-09-01T09:15:00Z")
    }
]);

// 2. Відгуки (reviews) з динамічними полями
db.reviews.deleteMany({
    productId: { $in: [NumberLong("5001"), NumberLong("5002"), NumberLong("5004")] }
});

db.reviews.insertMany([
    // Відгук 1: Двигун (містить параметри диностенду, коментарі та фото)
    {
        _id: new ObjectId("66f0c2000000000000000001"),
        productId: NumberLong("5001"),
        productName: "Електромотор Nissan Leaf EM57 80kW",
        rating: 5,
        text: "Двигун у відмінному стані, встановили в BMW E30 через перехідну плиту. Крутний момент відчувається миттєво!",
        author: {
            userId: "usr-201",
            displayName: "Іван Захарчук",
            isVerifiedBuyer: true
        },
        photos: [
            "https://cdn.evshop.local/reviews/e30_leaf_motor_bay.webp",
            "https://cdn.evshop.local/reviews/dyno_chart_em57.webp"
        ],
        // динамічні поля мотора:
        dynoTestedTorqueNm: 254.5,
        maxMeasuredRpm: 10200,
        coolingTypeUsed: "Liquid",
        comments: [
            {
                author: "Команда підтримки EVShop",
                text: "Вітаємо з успішним запуском проекту! Яку температуру інвертора зафіксували?",
                createdAt: new Date("2026-08-20T11:00:00Z")
            },
            {
                author: "Іван Захарчук",
                text: "Вище 55 градусів навіть при тривалому навантаженні не піднімається.",
                createdAt: new Date("2026-08-20T12:30:00Z")
            }
        ],
        discussionId: disc1Id,
        createdAt: new Date("2026-08-19T10:00:00Z")
    },

    // Відгук 2: Батарея (містить реальний запас ходу та зимовий тест)
    {
        _id: new ObjectId("66f0c2000000000000000002"),
        productId: NumberLong("5002"),
        productName: "Батарейний модуль CATL 12S 44V 120Ah 5.3kWh",
        rating: 4,
        text: "Склали 8 модулів у збірку на 350V (~42 kWh). Балансування чудове, проте знадобилося підбирати радіатори охолодження.",
        author: {
            userId: "usr-202",
            displayName: "Олена Петренко",
            isVerifiedBuyer: true
        },
        // динамічні поля батареї:
        testedCapacityKWh: 5.24,
        realWorldRangeKm: 215,
        testedWinterTempC: -8,
        donorVehicle: "Volkswagen Golf Mk2",
        comments: [],
        discussionId: disc2Id,
        createdAt: new Date("2026-09-05T16:45:00Z")
    },

    // Відгук 3: Механічна плита (містить час монтажу та модель коробки)
    {
        _id: new ObjectId("66f0c2000000000000000003"),
        productId: NumberLong("5004"),
        productName: "Комплект перехідної плити КПП BMW E30 / Getrag",
        rating: 5,
        text: "Ідеальна підгонка! Отвори під шпильки співпали до сотих часток міліметра. Первинний вал увійшов у шліци без перекосів.",
        author: {
            userId: "usr-203",
            displayName: "Сергій Коваль",
            isVerifiedBuyer: true
        },
        gearboxModel: "Getrag 260",
        installationHours: 3.5,
        fitmentAccuracyRating: 10,
        comments: [],
        discussionId: null,
        createdAt: new Date("2026-09-12T08:20:00Z")
    }
]);

// 3. Портфоліо переобладнаних авто (conversion_showcases)
db.conversion_showcases.deleteMany({
    title: { $in: ["BMW E30 Electric Retrofit 80kW", "VW Golf Mk2 City EV"] }
});

db.conversion_showcases.insertMany([
    {
        title: "BMW E30 Electric Retrofit 80kW",
        vehicle: {
            make: "BMW",
            model: "E30 Coupe",
            year: 1988
        },
        owner: {
            userId: "usr-201",
            displayName: "Іван Захарчук"
        },
        status: "Completed",
        usedProductIds: [NumberLong("5001"), NumberLong("5003"), NumberLong("5004")],
        specs: {
            topSpeedKmH: 155,
            acceleration0to100Sec: 6.8,
            batteryPackCapacityKWh: 42.0,
            curbWeightKg: 1180
        },
        milestones: [
            { stage: "Демонтаж ДВЗ M20B20", completedAt: new Date("2026-06-10") },
            { stage: "Монтаж електромотора EM57 та плити", completedAt: new Date("2026-07-02") },
            { stage: "Тестовий виїзд на полігон", completedAt: new Date("2026-08-18") }
        ],
        createdAt: new Date("2026-08-25T10:00:00Z")
    },
    {
        title: "VW Golf Mk2 City EV",
        vehicle: {
            make: "Volkswagen",
            model: "Golf Mk2",
            year: 1990
        },
        owner: {
            userId: "usr-202",
            displayName: "Олена Петренко"
        },
        status: "Testing",
        usedProductIds: [NumberLong("5001"), NumberLong("5002")],
        specs: {
            topSpeedKmH: 130,
            batteryPackCapacityKWh: 32.0,
            curbWeightKg: 1050
        },
        milestones: [
            { stage: "Зварювання кошика акумуляторного блоку", completedAt: new Date("2026-08-30") }
        ],
        createdAt: new Date("2026-09-08T15:00:00Z")
    }
]);

print("Тестові дані успішно завантажено в MongoDB!");
