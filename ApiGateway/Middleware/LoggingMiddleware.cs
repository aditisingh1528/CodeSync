namespace ApiGateway.Middleware;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "[Gateway ▶ REQUEST]  {Method} {Path}{Query}  |  {Time}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            startTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));

        await _next(context);

        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

        _logger.LogInformation(
            "[Gateway ◀ RESPONSE] {Method} {Path}  |  Status: {Status}  |  Elapsed: {Elapsed}ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            elapsed.ToString("F1"));
    }
}
