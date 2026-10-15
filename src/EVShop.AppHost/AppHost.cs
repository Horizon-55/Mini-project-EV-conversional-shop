var builder = DistributedApplication.CreateBuilder(args);

//Існуюча база даних MS SQL Server
var ordersDb = builder.AddConnectionString("OrdersDb");

// Сервіс Ordering API (тришарова архітектура: API -> BLL -> DAL)
builder.AddProject<Projects.EVShop_Ordering_Api>("orders-api")
    .WithReference(ordersDb);

builder.Build().Run();
