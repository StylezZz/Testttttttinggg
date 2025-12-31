using System.Collections.Concurrent;

namespace RiskListScraperAPI.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, ClientRequestInfo> _clients = new();
    private const int MAX_REQUESTS_PER_MINUTE = 20;
    private const int TIME_WINDOW_SECONDS = 60;

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for Swagger and health endpoints
        if (context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        var clientInfo = _clients.GetOrAdd(clientId, _ => new ClientRequestInfo());

        lock (clientInfo)
        {
            var now = DateTime.UtcNow;

            // Remove old requests outside the time window
            clientInfo.RequestTimestamps.RemoveAll(t => (now - t).TotalSeconds > TIME_WINDOW_SECONDS);

            if (clientInfo.RequestTimestamps.Count >= MAX_REQUESTS_PER_MINUTE)
            {
                var oldestRequest = clientInfo.RequestTimestamps.Min();
                var resetTime = oldestRequest.AddSeconds(TIME_WINDOW_SECONDS);
                var retryAfter = (int)(resetTime - now).TotalSeconds;

                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = retryAfter.ToString();
                context.Response.Headers["X-RateLimit-Limit"] = MAX_REQUESTS_PER_MINUTE.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = "0";
                context.Response.Headers["X-RateLimit-Reset"] = new DateTimeOffset(resetTime).ToUnixTimeSeconds().ToString();

                context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = $"Rate limit exceeded. Maximum {MAX_REQUESTS_PER_MINUTE} requests per minute allowed.",
                    errors = new[] { $"Please retry after {retryAfter} seconds." }
                }).Wait();
                return;
            }

            clientInfo.RequestTimestamps.Add(now);

            // Add rate limit headers
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-RateLimit-Limit"] = MAX_REQUESTS_PER_MINUTE.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] =
                    (MAX_REQUESTS_PER_MINUTE - clientInfo.RequestTimestamps.Count).ToString();
                return Task.CompletedTask;
            });
        }

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Use API Key if available, otherwise use IP address
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            return $"apikey_{apiKey}";
        }

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip_{ipAddress}";
    }

    private class ClientRequestInfo
    {
        public List<DateTime> RequestTimestamps { get; set; } = new();
    }
}
