// Приклади запитів та агрегацій у MongoDB
// 1. Агрегація середнього рейтингу
// 2. $lookup (об'єднання колекцій за посиланням)
// 3. Пошук за специфічними динамічними полями

const db = db.getSiblingDB("evshop_reviews");

print("--- 1. Агрегація: середній рейтинг та кількість відгуків для товарів ---");
const ratingStats = db.reviews.aggregate([
    {
        $group: {
            _id: "$productId",
            productName: { $first: "$productName" },
            averageRating: { $avg: "$rating" },
            totalReviews: { $sum: 1 },
            verifiedBuyersCount: {
                $sum: { $cond: ["$author.isVerifiedBuyer", 1, 0] }
            }
        }
    },
    {
        $project: {
            productId: "$_id",
            productName: 1,
            averageRating: { $round: ["$averageRating", 2] },
            totalReviews: 1,
            verifiedBuyersCount: 1,
            _id: 0
        }
    },
    { $sort: { averageRating: -1 } }
]).toArray();

printjson(ratingStats);

print("--- 2. Об'єднання ($lookup) між відгуками та темами обговорень ---");
const reviewsWithDiscussions = db.reviews.aggregate([
    { $match: { discussionId: { $ne: null } } },
    {
        $lookup: {
            from: "discussions",
            localField: "discussionId",
            foreignField: "_id",
            as: "relatedDiscussion"
        }
    },
    { $unwind: "$relatedDiscussion" },
    {
        $project: {
            productName: 1,
            rating: 1,
            reviewer: "$author.displayName",
            discussionTitle: "$relatedDiscussion.title",
            discussionTopic: "$relatedDiscussion.topic"
        }
    }
]).toArray();

printjson(reviewsWithDiscussions);

print("--- 3. Пошук за динамічними полями (тест на диностенді > 200 Нм) ---");
const dynoReviews = db.reviews.find({
    dynoTestedTorqueNm: { $exists: true, $gt: 200 }
}, {
    productName: 1,
    dynoTestedTorqueNm: 1,
    "author.displayName": 1
}).toArray();

printjson(dynoReviews);
