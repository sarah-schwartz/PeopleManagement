using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PeopleManagement.Infrastructure.Middleware;

public sealed class GlobalExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception.");
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception _)
    {
        if (context.Response.HasStarted)
        {
            throw new InvalidOperationException("The response has already started; cannot rewrite the response body.");
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        if (PreferHtmlError(context))
        {
            context.Response.Redirect("/People/Error");
            return;
        }

        context.Response.ContentType = MediaTypeNames.Application.Json;

        var payload = new ErrorResponse("An unexpected error occurred.");
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static bool PreferHtmlError(HttpContext context)
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/api"))
            return false;

        if (path.StartsWithSegments("/swagger"))
            return false;

        var accept = context.Request.Headers.Accept.ToString();
        return accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record ErrorResponse(string Message);

public static class GlobalExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
}
