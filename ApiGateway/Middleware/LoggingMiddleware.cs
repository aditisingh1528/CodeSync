namespace ApiGateway.Middleware;

/// <summary>
/// LOGGING MIDDLEWARE  (UC-4 Step 3a)
/// ====================================
/// Every HTTP request that enters the gateway is logged BEFORE it is
/// forwarded downstream, and again AFTER the downstream response comes back.
///
/// What we log:
///   ▶ Incoming  — timestamp, HTTP method, full path + query string
///   ◀ Outgoing  — HTTP status code, time elapsed (ms)
///
/// Why a custom middleware instead of just using built-in request logging?
///   • We log the exact gateway path (/auth/login) so you can see what
///     the CLIENT sent, not what Ocelot rewrote it to.
///   • We include elapsed ms so you can spot slow downstream calls.
///   • You can extend it later (log JWT subject, correlation-id, etc.).
/// </summary>
public class LoggingMiddleware
{
    private readonly RequestDelegate _next;   // next middleware in the pipeline
    private readonly ILogger<LoggingMiddleware> _logger;

    // ASP.NET Core DI injects these through the constructor.
    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    /// <summary>
    /// InvokeAsync is called by ASP.NET Core for every request.
    /// We wrap _next(context) so we can measure time and capture the status code.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // ── BEFORE forwarding ──────────────────────────────────────────────
        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "[Gateway ▶ REQUEST]  {Method} {Path}{Query}  |  {Time}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            startTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));

        // ── Call the next middleware (Ocelot routing happens here) ─────────
        await _next(context);

        // ── AFTER response is returned from downstream ────────────────────
        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

        _logger.LogInformation(
            "[Gateway ◀ RESPONSE] {Method} {Path}  |  Status: {Status}  |  Elapsed: {Elapsed}ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            elapsed.ToString("F1"));
    }
}
