using Microsoft.Extensions.Options;
using ConfigAndSecurity.Config;
using ConfigAndSecurity.Middlewares;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. Иерархическая конфигурация
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

// 2. Регистрация настроек и Fail-Fast валидация
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));
builder.Services.AddSingleton<IValidateOptions<AppOptions>, AppOptionsValidator>();
builder.Services.AddOptions<AppOptions>()
    .BindConfiguration(AppOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var settings = builder.Configuration.GetSection(AppOptions.SectionName).Get<AppOptions>();

// 3. CORS защита
builder.Services.AddCors(options =>
{
    options.AddPolicy("TrustedOnly", policy =>
    {
        var origins = settings?.TrustedOrigins?.ToArray() ?? Array.Empty<string>();
        if (origins.Length == 0)
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins(origins);
        }
        policy.AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 4. Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = settings?.RateLimit?.GlobalPermitLimit ?? 100,
            Window = TimeSpan.FromMinutes(settings?.RateLimit?.WindowMinutes ?? 1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    options.AddFixedWindowLimiter("Strict", opt =>
    {
        opt.PermitLimit = settings?.RateLimit?.StrictPermitLimit ?? 5;
        opt.Window = TimeSpan.FromMinutes(settings?.RateLimit?.WindowMinutes ?? 1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = "Too many requests",
            message = "Превышен лимит запросов. Пожалуйста, повторите попытку позже.",
            retryAfter = 60
        };

        await context.HttpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);
    };
});

var app = builder.Build();

// Порядок Middleware
app.UseRateLimiter();
app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors("TrustedOnly");

// Endpoints
app.MapGet("/api/items", () => Results.Ok(new[] { "Item 1", "Item 2" }));
app.MapGet("/api/error", () => { throw new Exception("Тестовая ошибка"); });
app.MapPost("/api/items", () => Results.Created("/api/items/1", new { Id = 1, Name = "New Item" }))
   .RequireRateLimiting("Strict");

app.Run();

public partial class Program { }