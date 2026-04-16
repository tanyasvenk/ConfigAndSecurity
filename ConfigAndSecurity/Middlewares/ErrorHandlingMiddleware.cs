using Microsoft.Extensions.Options;
using ConfigAndSecurity.Config;
using ConfigAndSecurity.Domain;

namespace ConfigAndSecurity.Middlewares;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<AppOptions> options)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var requestId = context.Items["RequestId"]?.ToString() ?? "unknown";
            _logger.LogError(ex, "Ошибка (RequestId: {RequestId})", requestId);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            ErrorResponse errorResponse;
            if (options.Value.Mode == AppMode.Production)
            {
                errorResponse = new ErrorResponse("Внутренняя ошибка сервера", requestId);
            }
            else
            {
                errorResponse = new ErrorResponse(ex.Message, requestId);
            }

            await context.Response.WriteAsJsonAsync(errorResponse);
        }
    }
}