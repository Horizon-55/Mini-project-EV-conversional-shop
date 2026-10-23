var builder = DistributedApplication.CreateBuilder(args);

// 1. Рядки підключення до реляційних баз даних MS SQL Server
var ordersDb = builder.AddConnectionString("OrdersDb");
var catalogDb = builder.AddConnectionString("CatalogDb");

// 2. Сервіс Ordering API (Лабораторна робота №2: ADO.NET + Dapper)
builder.AddProject<Projects.EVShop_Ordering_Api>("orders-api")
    .WithReference(ordersDb);

// 3. Сервіс Catalog API (Лабораторна робота №3: Entity Framework Core Code-First)
builder.AddProject<Projects.EVShop_Catalog_Api>("catalog-api")
    .WithReference(catalogDb);

builder.Build().Run();
