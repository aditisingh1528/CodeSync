using System.Net;
using System.Text.Json;

namespace ApiGateway.Middleware;

/// <summary>
/// EXCEPTION HANDLING MIDDLEWARE  (UC-4 Step 3b)
/// ================================================
/// Catches any unhandled exception thrown anywhere in the pipeline
/// (including inside Ocelot or the logging middleware) and converts
/// it into a clean JSON error response so the client always gets a
/// structured body, not an HTML error page or raw stack trace.
///
/// Response format:
///   {
///     "statusCode": 500,
///     "message":    "An unexpected error occurred.",
///     "detail":     "..."   // only in Development; hidden in Production
///   }
///
/// Design principle (defence-in-depth):
///   This middleware sits at the VERY FRONT of the pipeline so it wraps
///   everything else — any exception anywhere gets caught here.
/// </summary>
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
            // Normal path — let the rest of the pipeline run.
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the FULL exception server-side so we can debug it.
            _logger.LogError(ex,
                "[Gateway ❌ EXCEPTION] {Method} {Path} — {Message}",
                context.Request.Method,
                context.Request.Path,
                ex.Message);

            // Write a clean JSON response to the client.
            await WriteErrorResponseAsync(context, ex);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Builds the JSON error response.
    // In Development we expose the exception message and type to help debug.
    // In Production we hide internals and show a generic message.
    // ─────────────────────────────────────────────────────────────────────
    private async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;

        object responseBody;

        if (_env.IsDevelopment())
        {
            // Full detail in development — helps you find the bug fast.
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
            // Safe, generic message in production — never leak internals.
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
