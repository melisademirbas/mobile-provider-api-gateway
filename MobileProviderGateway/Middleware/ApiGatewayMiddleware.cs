using System.Net.Http.Headers;

namespace MobileProviderGateway.Middleware;

public class ApiGatewayMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiGatewayMiddleware> _logger;
    private readonly string _downstreamApiUrl = "http://localhost:5096";

    public ApiGatewayMiddleware(
        RequestDelegate next,
        IHttpClientFactory httpClientFactory,
        ILogger<ApiGatewayMiddleware> logger)
    {
        _next = next;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Sadece /api/v1/ ile başlayan istekleri gateway üzerinden yönlendir
        if (context.Request.Path.StartsWithSegments("/api/v1"))
        {
            await ForwardRequestAsync(context);
        }
        else
        {
            await _next(context);
        }
    }

    private async Task ForwardRequestAsync(HttpContext context)
    {
        var client = _httpClientFactory.CreateClient();
        
        // Downstream API URL'ini oluştur
        var downstreamUrl = $"{_downstreamApiUrl}{context.Request.Path}{context.Request.QueryString}";
        
        _logger.LogInformation($"Forwarding request: {context.Request.Method} {downstreamUrl}");

        // Request oluştur
        var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), downstreamUrl);

        // Headers'ı kopyala (Authorization dahil) - GET istekleri için body yok
        foreach (var header in context.Request.Headers)
        {
            if (!header.Key.StartsWith(":") && 
                header.Key != "Host" && 
                header.Key != "Content-Length" &&
                header.Key != "Transfer-Encoding")
            {
                // Authorization header'ını özellikle ekle
                if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", header.Value.ToString().Replace("Bearer ", ""));
                }
                else
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }
        }

        // POST/PUT için body kopyala
        if (context.Request.Method == "POST" || context.Request.Method == "PUT" || context.Request.Method == "PATCH")
        {
            if (context.Request.ContentLength > 0)
            {
                var bodyStream = new MemoryStream();
                await context.Request.Body.CopyToAsync(bodyStream);
                bodyStream.Position = 0;
                request.Content = new StreamContent(bodyStream);
                
                if (context.Request.ContentType != null)
                {
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);
                }
            }
        }

        try
        {
            // Downstream API'ye istek gönder
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

            // Response'u kopyala
            context.Response.StatusCode = (int)response.StatusCode;
            
            // Response headers'ı kopyala (Content-Type hariç - aşağıda ayarlanacak)
            foreach (var header in response.Headers)
            {
                if (!header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }
            }

            // Content headers'ı kopyala
            foreach (var header in response.Content.Headers)
            {
                if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.ContentType = header.Value.FirstOrDefault();
                }
                else if (!header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }
            }

            // Response body'yi kopyala
            var responseBody = await response.Content.ReadAsByteArrayAsync();
            await context.Response.Body.WriteAsync(responseBody, 0, responseBody.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error forwarding request to downstream API: {downstreamUrl}");
            context.Response.StatusCode = 502; // Bad Gateway
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync($"{{\"error\":\"Gateway error: {ex.Message}\"}}");
        }
    }
}

