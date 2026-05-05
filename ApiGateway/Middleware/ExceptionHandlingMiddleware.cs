using System.Net;
using System.Text.Json;

namespace ApiGateway.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next   = next;
        _logger = logger;
        _env    = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Normal path
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Gateway ❌ EXCEPTION] {Method} {Path} — {Message}",
                context.Request.Method,
                context.Request.Path,
                ex.Message);

            await WriteErrorResponseAsync(context, ex);
        }
    }
    private async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;

        object responseBody;

        if (_env.IsDevelopment())
        {
            responseBody = new
            {
                statusCode    = 500,
                message       = "An unhandled exception occurred in the gateway.",
                exceptionType = ex.GetType().Name,
                detail        = ex.Message,
                inner         = ex.InnerException?.Message
            };
        }
        else
        {
            responseBody = new
            {
                statusCode = 500,
                message    = "An unexpected error occurred. Please try again later."
            };
        }

        var json = JsonSerializer.Serialize(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
