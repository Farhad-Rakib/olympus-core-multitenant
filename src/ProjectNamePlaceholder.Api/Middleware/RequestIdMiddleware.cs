using Serilog.Context;

namespace ProjectNamePlaceholder.Api.Middleware;

// Gives every request a stable id, echoed back on the response so a customer can quote it in a
// support ticket and it can be grepped out of the logs. Honors an incoming X-Request-Id (a proxy
// or the calling frontend may already have assigned one) so the id survives across hops.
public sealed class RequestIdMiddleware
{
    public const string HeaderName = "X-Request-Id";

    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();

        // An untrusted caller controls this value, so it is length-capped and never used for
        // anything but correlation -- it must not reach a log sink as an unbounded blob.
        var requestId = string.IsNullOrWhiteSpace(incoming) || incoming.Length > 128
            ? context.TraceIdentifier
            : incoming;

        context.Response.Headers[HeaderName] = requestId;

        using (LogContext.PushProperty("RequestId", requestId))
        {
            await _next(context);
        }
    }
}

public static class RequestIdMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestId(this IApplicationBuilder app)
        => app.UseMiddleware<RequestIdMiddleware>();
}
