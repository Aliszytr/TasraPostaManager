using System.Net;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace TasraPostaManager.Middleware;

/// <summary>
/// Global exception handling middleware.
/// Beklenmedik hataları yakalar, loglar ve kullanıcıya anlamlı hata sayfası gösterir.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            // 404 gibi başarısız durum kodlarını yönet
            if (context.Response.StatusCode == (int)HttpStatusCode.NotFound
                && !context.Response.HasStarted
                && !context.Request.Path.StartsWithSegments("/api"))
            {
                await HandleNotFoundAsync(context);
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var errorId = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        _logger.LogError(exception,
            "İşlenmeyen hata oluştu. ErrorId={ErrorId}, Path={Path}, Method={Method}",
            errorId, context.Request.Path, context.Request.Method);

        if (context.Response.HasStarted)
        {
            _logger.LogWarning(
                "Response zaten başlamış, hata sayfası gösterilemiyor. ErrorId={ErrorId}", errorId);
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "text/html; charset=utf-8";

        var showDetails = _env.IsDevelopment();
        var html = BuildErrorHtml(errorId, exception, showDetails, statusCode: 500);
        await context.Response.WriteAsync(html);
    }

    private async Task HandleNotFoundAsync(HttpContext context)
    {
        var errorId = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        _logger.LogWarning(
            "Sayfa bulunamadı. ErrorId={ErrorId}, Path={Path}",
            errorId, context.Request.Path);

        context.Response.ContentType = "text/html; charset=utf-8";

        var html = BuildErrorHtml(errorId, exception: null, showDetails: false, statusCode: 404);
        await context.Response.WriteAsync(html);
    }

    private static string BuildErrorHtml(string errorId, Exception? exception, bool showDetails, int statusCode)
    {
        var title = statusCode switch
        {
            404 => "Sayfa Bulunamadı",
            500 => "Sunucu Hatası",
            _ => "Bir Hata Oluştu"
        };

        var icon = statusCode switch
        {
            404 => "fa-search",
            500 => "fa-exclamation-triangle",
            _ => "fa-exclamation-circle"
        };

        var message = statusCode switch
        {
            404 => "Aradığınız sayfa bulunamadı. Lütfen URL'yi kontrol edin veya ana sayfaya dönün.",
            500 => "Beklenmedik bir sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin.",
            _ => "Bir hata oluştu. Lütfen daha sonra tekrar deneyin."
        };

        var detailsSection = showDetails && exception != null
            ? $"""
               <div class="error-details">
                   <h4>Hata Detayları (Sadece Development)</h4>
                   <p><strong>Tür:</strong> {exception.GetType().FullName}</p>
                   <p><strong>Mesaj:</strong> {System.Net.WebUtility.HtmlEncode(exception.Message)}</p>
                   <pre>{System.Net.WebUtility.HtmlEncode(exception.StackTrace ?? "Stack trace yok")}</pre>
               </div>
               """
            : "";

        return $$"""
            <!DOCTYPE html>
            <html lang="tr">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>{{title}} - TasraPostaManager</title>
                <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet" />
                <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css" rel="stylesheet" />
                <style>
                    body {
                        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                        min-height: 100vh;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                    }
                    .error-card {
                        background: rgba(255,255,255,0.95);
                        border-radius: 20px;
                        box-shadow: 0 20px 60px rgba(0,0,0,0.15);
                        padding: 3rem;
                        max-width: 600px;
                        width: 90%;
                        text-align: center;
                        backdrop-filter: blur(10px);
                    }
                    .error-icon {
                        font-size: 4rem;
                        color: {{(statusCode == 404 ? "#6c757d" : "#dc3545")}};
                        margin-bottom: 1.5rem;
                    }
                    .error-code {
                        font-size: 5rem;
                        font-weight: 800;
                        color: {{(statusCode == 404 ? "#6c757d" : "#dc3545")}};
                        line-height: 1;
                        margin-bottom: 0.5rem;
                    }
                    .error-title {
                        font-size: 1.5rem;
                        font-weight: 600;
                        color: #333;
                        margin-bottom: 1rem;
                    }
                    .error-message {
                        color: #666;
                        margin-bottom: 1.5rem;
                        line-height: 1.6;
                    }
                    .error-id {
                        font-size: 0.85rem;
                        color: #999;
                        margin-top: 1rem;
                    }
                    .btn-home {
                        background: linear-gradient(135deg, #667eea, #764ba2);
                        color: white;
                        border: none;
                        padding: 0.75rem 2rem;
                        border-radius: 50px;
                        font-weight: 600;
                        text-decoration: none;
                        transition: all 0.3s ease;
                    }
                    .btn-home:hover {
                        transform: translateY(-2px);
                        box-shadow: 0 5px 20px rgba(102,126,234,0.4);
                        color: white;
                    }
                    .error-details {
                        text-align: left;
                        background: #f8f9fa;
                        border-radius: 10px;
                        padding: 1.5rem;
                        margin-top: 1.5rem;
                        font-size: 0.85rem;
                    }
                    .error-details pre {
                        background: #2d3436;
                        color: #dfe6e9;
                        padding: 1rem;
                        border-radius: 8px;
                        font-size: 0.75rem;
                        max-height: 300px;
                        overflow-y: auto;
                    }
                </style>
            </head>
            <body>
                <div class="error-card">
                    <div class="error-icon"><i class="fas {{icon}}"></i></div>
                    <div class="error-code">{{statusCode}}</div>
                    <div class="error-title">{{title}}</div>
                    <p class="error-message">{{message}}</p>
                    <a href="/" class="btn-home">
                        <i class="fas fa-home me-2"></i>Ana Sayfaya Dön
                    </a>
                    <div class="error-id">
                        <i class="fas fa-fingerprint me-1"></i>Hata Referans: {{errorId}}
                    </div>
                    {{detailsSection}}
                </div>
            </body>
            </html>
            """;
    }
}

/// <summary>
/// Extension: Middleware pipeline'a kolayca eklemek için.
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
