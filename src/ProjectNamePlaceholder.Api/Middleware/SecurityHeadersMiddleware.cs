namespace ProjectNamePlaceholder.Api.Middleware;

// Sets the baseline response security headers. Deliberately minimal: this is an API that serves
// JSON, not HTML, so the CSP only needs to be restrictive enough to neuter a response that a
// browser is tricked into rendering as a document -- it is not the CSP the React app ships with
// (that belongs on whatever serves the frontend's index.html).
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // frame-ancestors 'none' is the modern equivalent of X-Frame-Options: DENY; both are sent
        // because older browsers ignore the CSP directive.
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        headers["X-Frame-Options"] = "DENY";
        headers["X-Content-Type-Options"] = "nosniff";
        headers["Referrer-Policy"] = "no-referrer";

        return _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
