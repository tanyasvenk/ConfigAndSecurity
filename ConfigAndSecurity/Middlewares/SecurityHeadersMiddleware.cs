namespace ConfigAndSecurity.Middlewares;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey("X-Frame-Options"))
                context.Response.Headers.Append("X-Frame-Options", "DENY");

            if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

            if (!context.Response.Headers.ContainsKey("X-XSS-Protection"))
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

            if (!context.Response.Headers.ContainsKey("Referrer-Policy"))
                context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            return Task.CompletedTask;
        });

        await _next(context);
    }
}