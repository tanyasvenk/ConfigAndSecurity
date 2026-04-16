namespace ConfigAndSecurity.Middlewares;

public class RequestIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestIdMiddleware> _logger;

    public RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString();

        context.Items["RequestId"] = requestId;

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey("X-Request-Id"))
                context.Response.Headers.Append("X-Request-Id", requestId);
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = requestId }))
        {
            await _next(context);
        }
    }
}