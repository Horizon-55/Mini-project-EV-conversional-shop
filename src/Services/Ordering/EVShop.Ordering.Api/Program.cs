using AutoMapper;
using EVShop.Ordering.Api.Middleware;
using EVShop.Ordering.Bll.Interfaces;
using EVShop.Ordering.Bll.Mapping;
using EVShop.Ordering.Bll.Services;
using EVShop.Ordering.Dal;
using EVShop.Ordering.Dal.Interfaces;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Налаштування Serilog логування (Крок A)
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// 2. Читання рядка підключення з підтримкою змінних оточення (Крок A)
var connectionString = builder.Configuration.GetConnectionString("OrdersDb")
    ?? throw new InvalidOperationException("Connection string 'OrdersDb' is missing");

// 3. Реєстрація Unit of Work та сервісів бізнес-логіки BLL (Крок A, B.4, C.2)
builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

// 4. Реєстрація AutoMapper (Крок C.1)
builder.Services.AddSingleton<IMapper>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var mapperConfig = new MapperConfiguration(cfg =>
    {
        cfg.AddProfile<OrderProfile>();
    }, loggerFactory);
    return mapperConfig.CreateMapper();
});

// 5. Контролери та централізована обробка ProblemDetails (Крок D)
builder.Services.AddControllers();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// 6. Документація OpenAPI / Swagger (Крок D)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "EVShop Ordering API (Лаб 2)",
        Version = "v1",
        Description = "Тришарова архітектура мікросервісу замовлень на ADO.NET та Dapper"
    });
});

var app = builder.Build();

// Конвеєр обробки запитів (Middleware Pipeline)
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EVShop Ordering API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

// Робимо доступним для тестів WebApplicationFactory
public partial class Program { }
