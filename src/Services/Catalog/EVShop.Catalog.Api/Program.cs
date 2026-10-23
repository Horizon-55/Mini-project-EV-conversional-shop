using AutoMapper;
using EVShop.Catalog.Api.Middleware;
using EVShop.Catalog.Bll.Interfaces;
using EVShop.Catalog.Bll.Mapping;
using EVShop.Catalog.Bll.Services;
using EVShop.Catalog.Bll.Validators;
using EVShop.Catalog.Dal.Context;
using EVShop.Catalog.Dal.Data;
using EVShop.Catalog.Dal.Interfaces;
using EVShop.Catalog.Dal.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// 1. Налаштування логування Serilog з показом SQL-команд EF Core (Вимога 4.5 методички)
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// 2. Рядок підключення та Entity Framework Core DbContext (Вимога Частина 1)
var connectionString = builder.Configuration.GetConnectionString("CatalogDb")
    ?? throw new InvalidOperationException("Connection string 'CatalogDb' is missing from configuration");

builder.Services.AddDbContext<CatalogDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// 3. Реєстрація Unit of Work та репозиторіїв (Вимога 2.3 методички)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// 4. Реєстрація AutoMapper з профілем каталогу (Вимога Частина 3)
builder.Services.AddSingleton<IMapper>(sp =>
{
    var mapperConfig = new MapperConfiguration(cfg =>
    {
        cfg.AddProfile<CatalogProfile>();
    }, sp.GetService<ILoggerFactory>());
    return mapperConfig.CreateMapper();
});

// 5. Реєстрація FluentValidation валідаторів (Вимога 4.3 методички)
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductDtoValidator>();

// 6. CORS-політика
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 7. Контролери та централізована обробка ProblemDetails (Вимога 4.4)
builder.Services.AddControllers();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// 8. Swagger / OpenAPI документація
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "EVShop Catalog Service API",
        Version = "v1",
        Description = "Лабораторна робота №3: Тришарова архітектура на базі Entity Framework Core (Code First, Fluent API, Paging/Filtering/Sorting, Specifications, Idempotent Seeding)"
    });
});

var app = builder.Build();

// Конвеєр обробки запитів (Middleware Pipeline)
app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EVShop Catalog API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHttpsRedirection();
}

app.MapControllers();

// Автоматичне застосування міграцій та ідемпотентний сідінг при старті (Вимога 4.1 методички)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    try
    {
        Log.Information("Застосування міграцій EF Core до бази даних CatalogDb...");
        await db.Database.MigrateAsync();
        Log.Information("Міграції успішно застосовано.");

        if (!app.Environment.IsProduction())
        {
            Log.Information("Перевірка та виконання початкового наповнення (Seeding)...");
            await DbInitializer.SeedAsync(db);
            Log.Information("Початкове наповнення завершено.");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Помилка під час ініціалізації бази даних CatalogDb");
    }
}

app.Run();
