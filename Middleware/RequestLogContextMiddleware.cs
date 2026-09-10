using System.IdentityModel.Tokens.Jwt;

namespace laoyu_blog_backend.Middleware;

public sealed class RequestLogContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLogContextMiddleware> _logger;

    public RequestLogContextMiddleware(
        RequestDelegate next,
        ILogger<RequestLogContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirst(
            JwtRegisteredClaimNames.Sub)?.Value
            ?? "anonymous";

        using (_logger.BeginScope(
            "TraceId: {TraceId} UserId: {UserId}",
            context.TraceIdentifier,
            userId))
        {
            await _next(context);
        }
    }
}
