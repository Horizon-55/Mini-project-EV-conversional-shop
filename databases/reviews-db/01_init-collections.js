// Лабораторна робота №1. Ініціалізація бази відгуків та спільноти (MongoDB)
// Демонструє валідацію схеми JSON Schema, індекси та NoSQL моделювання

const dbName = "evshop_reviews";
const db = db.getSiblingDB(dbName);

print("Створення колекцій та правил валідації для бази: " + dbName);

// 1. Колекція відгуків (reviews)
if (db.getCollectionNames().includes("reviews")) {
    db.reviews.drop();
}

db.createCollection("reviews", {
    validator: {
        $jsonSchema: {
            bsonType: "object",
            required: ["productId", "productName", "rating", "author", "createdAt"],
            properties: {
                productId: {
                    bsonType: ["int", "long"],
                    description: "Посилання на товар з каталогу (без зовнішнього ключа)"
                },
                productName: {
                    bsonType: "string",
                    description: "Денормалізована назва товару для швидкого читання"
                },
                rating: {
                    bsonType: "int",
                    minimum: 1,
                    maximum: 5,
                    description: "Оцінка товару від 1 до 5 зірок"
                },
                text: {
                    bsonType: "string",
                    description: "Текст відгуку клієнта"
                },
                author: {
                    bsonType: "object",
                    required: ["userId", "displayName"],
                    properties: {
                        userId: { bsonType: "string" },
                        displayName: { bsonType: "string" },
                        isVerifiedBuyer: { bsonType: "bool" }
                    }
                },
                comments: {
                    bsonType: "array",
                    description: "Вбудовані коментарі-відповіді (embedding)",
                    items: {
                        bsonType: "object",
                        required: ["author", "text", "createdAt"],
                        properties: {
                            author: { bsonType: "string" },
                            text: { bsonType: "string" },
                            createdAt: { bsonType: "date" }
                        }
                    }
                },
                discussionId: {
                    bsonType: ["objectId", "null"],
                    description: "Посилання на пов'язану тему на форумі (referencing)"
                },
                createdAt: {
                    bsonType: "date"
                }
            }
        }
    }
});

// Індекси для швидкого пошуку відгуків до товару та сортування
db.reviews.createIndex({ productId: 1, createdAt: -1 }, { name: "IX_Reviews_ProductId_CreatedAt" });
db.reviews.createIndex({ rating: 1 }, { name: "IX_Reviews_Rating" });
db.reviews.createIndex({ "author.userId": 1 }, { name: "IX_Reviews_AuthorUserId" });

// 2. Колекція тем форуму / обговорень (discussions)
if (db.getCollectionNames().includes("discussions")) {
    db.discussions.drop();
}

db.createCollection("discussions", {
    validator: {
        $jsonSchema: {
            bsonType: "object",
            required: ["title", "topic", "starter", "createdAt"],
            properties: {
                title: { bsonType: "string" },
                topic: { bsonType: "string" },
                starter: {
                    bsonType: "object",
                    required: ["userId", "displayName"],
                    properties: {
                        userId: { bsonType: "string" },
                        displayName: { bsonType: "string" }
                    }
                },
                createdAt: { bsonType: "date" }
            }
        }
    }
});

db.discussions.createIndex({ topic: 1, createdAt: -1 }, { name: "IX_Discussions_Topic_CreatedAt" });

// 3. Колекція готових проєктів переобладнання (conversion_showcases)
if (db.getCollectionNames().includes("conversion_showcases")) {
    db.conversion_showcases.drop();
}

db.createCollection("conversion_showcases", {
    validator: {
        $jsonSchema: {
            bsonType: "object",
            required: ["title", "vehicle", "owner", "status", "createdAt"],
            properties: {
                title: { bsonType: "string" },
                vehicle: {
                    bsonType: "object",
                    required: ["make", "model", "year"],
                    properties: {
                        make: { bsonType: "string" },
                        model: { bsonType: "string" },
                        year: { bsonType: "int" }
                    }
                },
                owner: {
                    bsonType: "object",
                    required: ["userId", "displayName"],
                    properties: {
                        userId: { bsonType: "string" },
                        displayName: { bsonType: "string" }
                    }
                },
                status: {
                    enum: ["In_Progress", "Completed", "Testing"]
                },
                usedProductIds: {
                    bsonType: "array",
                    description: "Список використаних компонентів"
                },
                createdAt: { bsonType: "date" }
            }
        }
    }
});

db.conversion_showcases.createIndex({ "vehicle.make": 1, "vehicle.model": 1 });
db.conversion_showcases.createIndex({ status: 1 });

print("Колекції та індекси в MongoDB успішно створено!");
