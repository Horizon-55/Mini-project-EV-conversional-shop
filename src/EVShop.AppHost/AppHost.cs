var builder = DistributedApplication.CreateBuilder(args);

// 1. Рядки підключення до реляційних баз даних MS SQL Server та документної MongoDB
var ordersDb = builder.AddConnectionString("OrdersDb");
var catalogDb = builder.AddConnectionString("CatalogDb");
var reviewsDb = builder.AddConnectionString("reviews-db");

// 2. Сервіс Ordering API (Лабораторна робота №2: ADO.NET + Dapper)
builder.AddProject<Projects.EVShop_Ordering_Api>("orders-api")
    .WithReference(ordersDb);

// 3. Сервіс Catalog API (Лабораторна робота №3: Entity Framework Core Code-First)
builder.AddProject<Projects.EVShop_Catalog_Api>("catalog-api")
    .WithReference(catalogDb);

// 4. Сервіс Reviews API (Лабораторна робота №4: Clean Architecture + MongoDB + CQRS)
builder.AddProject<Projects.EVShop_Reviews_Api>("reviews-api")
    .WithReference(reviewsDb);

builder.Build().Run();
