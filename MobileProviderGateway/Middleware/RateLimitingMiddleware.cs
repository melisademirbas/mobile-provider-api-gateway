using System.Collections.Concurrent;
using System.Security.Claims;

namespace MobileProviderGateway.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    // Rate limit storage: Key -> (RequestCount, ResetTime)
    private static readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore = new();

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        // Rate limiting sadece belirli endpoint'ler için
        if (path.Contains("/api/v1/bills/querybill"))
        {
            // QueryBill için: 3 istek/gün, SubscriberNo bazlı
            var clientId = GetClientId(context, "SubscriberNo");
            
            // Eğer SubscriberNo bulunamazsa, path'den almayı dene
            if (string.IsNullOrEmpty(clientId))
            {
                // Path formatı: /api/v1/Bills/QueryBill/{subscriberNo}
                var pathParts = path.Split('/');
                for (int i = 0; i < pathParts.Length; i++)
                {
                    if (pathParts[i].Equals("querybill", StringComparison.OrdinalIgnoreCase) && i + 1 < pathParts.Length)
                    {
                        clientId = pathParts[i + 1];
                        break;
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(clientId))
            {
                var limitKey = $"querybill_{clientId}";
                if (!CheckRateLimit(limitKey, limit: 3, periodSeconds: 86400)) // 1 gün = 86400 saniye
                {
                    _logger.LogWarning($"Rate limit exceeded for QueryBill. ClientId: {clientId}");
                    context.Response.StatusCode = 429; // Too Many Requests
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\":\"Rate limit exceeded. Maximum 3 requests per day for QueryBill endpoint.\"}");
                    return;
                }
            }
        }
        else if (path.StartsWith("/api/v1/"))
        {
            // Diğer endpoint'ler için: 5 istek/saniye, IP bazlı
            var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var limitKey = $"general_{clientId}";
            if (!CheckRateLimit(limitKey, limit: 5, periodSeconds: 1))
            {
                _logger.LogWarning($"Rate limit exceeded for general endpoint. IP: {clientId}");
                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\":\"Rate limit exceeded. Maximum 5 requests per second.\"}");
                return;
            }
        }

        await _next(context);
    }

    private string? GetClientId(HttpContext context, string claimType)
    {
        // JWT token'dan SubscriberNo claim'ini al
        var subscriberNoClaim = context.User?.FindFirst("SubscriberNo");
        if (subscriberNoClaim != null)
        {
            return subscriberNoClaim.Value;
        }

        // Path'den subscriberNo parametresini al (örn: /api/v1/Bills/QueryBill/1)
        var path = context.Request.Path.Value ?? "";
        var pathParts = path.Split('/');
        for (int i = 0; i < pathParts.Length; i++)
        {
            if (pathParts[i].Equals("QueryBill", StringComparison.OrdinalIgnoreCase) && i + 1 < pathParts.Length)
            {
                return pathParts[i + 1];
            }
        }

        return null;
    }

    private bool CheckRateLimit(string key, int limit, int periodSeconds)
    {
        var now = DateTime.UtcNow;
        var info = _rateLimitStore.GetOrAdd(key, _ => new RateLimitInfo { ResetTime = now.AddSeconds(periodSeconds) });

        // Reset time geçmişse, sıfırla
        if (now >= info.ResetTime)
        {
            info.RequestCount = 0;
            info.ResetTime = now.AddSeconds(periodSeconds);
        }

        // Limit kontrolü
        if (info.RequestCount >= limit)
        {
            return false;
        }

        // İsteği say
        info.RequestCount++;
        return true;
    }

    private class RateLimitInfo
    {
        public int RequestCount { get; set; }
        public DateTime ResetTime { get; set; }
    }
}

