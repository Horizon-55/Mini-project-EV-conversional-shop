using EVShop.Reviews.Api.Middleware;
using EVShop.Reviews.Application;
using EVShop.Reviews.Infrastructure;
using EVShop.Reviews.Infrastructure.Seeding;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog Logging Configuration
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// 2. Clean Architecture Layer Registrations
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 3. Controllers & ProblemDetails Error Handling (RFC 9457)
builder.Services.AddControllers();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// 4. CORS Policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 5. Swagger / OpenAPI Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "EVShop Reviews Microservice API",
        Version = "v1",
        Description = "Лабораторна робота №4: Clean Architecture на базі MongoDB, CQRS та MediatR (Rich Domain Model, Pipeline Behaviors, ProblemDetails RFC 9457, Optimistic Concurrency, Idempotent Seeding)"
    });
});

var app = builder.Build();

// 6. Request Pipeline Middleware
app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EVShop Reviews API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHttpsRedirection();
}

app.MapControllers();

// 7. Idempotent Data Seeding
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
    try
    {
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to seed Reviews collection during startup.");
    }
}

app.Run();
