/*
==========================================================================
  API GATEWAY — Program.cs   (UC-4)
==========================================================================
  This is the ENTRY POINT of the API Gateway project.
  It wires up:
    1. Ocelot package & routing config (ocelot.json)
    2. JWT authentication (same key/issuer/audience as AuthService)
    3. Custom middleware (exception handler + logger)
    4. The Ocelot middleware that actually does the reverse-proxying
==========================================================================
*/

using System.Text;
using ApiGateway.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

// ─────────────────────────────────────────────────────────────────────────
// STEP 1 — BUILD THE HOST
// ─────────────────────────────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

/*
  AddJsonFile("ocelot.json") loads our routing rules.
  optional: false  → crash on startup if the file is missing (you want to know).
  reloadOnChange: true → pick up ocelot.json edits without restarting the app.
*/
builder.Configuration
       .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// ─────────────────────────────────────────────────────────────────────────
// STEP 2 — JWT AUTHENTICATION  (UC-4 Step 4)
// ─────────────────────────────────────────────────────────────────────────
/*
  WHY JWT in the gateway?
  ────────────────────────
  Without this, any request to /user/* or /admin/* would be forwarded
  to AuthService and AuthService would reject it with 401.
  With this, the gateway rejects invalid tokens BEFORE touching AuthService —
  cheaper, faster, and a proper security boundary.

  IMPORTANT: The Jwt:Key / Issuer / Audience here MUST match the values
  used in AuthService's appsettings.json, otherwise a token issued by
  AuthService will fail to validate here.
*/
var jwtKey      = builder.Configuration["Jwt:Key"]!;
var jwtIssuer   = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)

    /*
      We register the scheme under the name "GatewayJwt".
      This string must match AuthenticationProviderKey in ocelot.json
      for every route that requires a valid JWT.
    */
    .AddJwtBearer("GatewayJwt", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtKey)),
            /*
              ClockSkew = Zero means a token is invalid the INSTANT it expires.
              The default is +5 minutes which can allow expired tokens through.
            */
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ─────────────────────────────────────────────────────────────────────────
// STEP 3 — ADD OCELOT  (UC-4 Step 1 & 2)
// ─────────────────────────────────────────────────────────────────────────
/*
  AddOcelot() registers all Ocelot internal services.
  Ocelot reads the Routes we defined in ocelot.json and creates
  a reverse-proxy handler for each one.
*/
builder.Services.AddOcelot(builder.Configuration);

// ─────────────────────────────────────────────────────────────────────────
// STEP 4 — LOGGING
// ─────────────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();     // Logs appear in the terminal window
builder.Logging.AddDebug();       // Logs appear in the VS/Rider debug output

// ─────────────────────────────────────────────────────────────────────────
// BUILD THE APP
// ─────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ─────────────────────────────────────────────────────────────────────────
// STEP 5 — MIDDLEWARE PIPELINE ORDER  (UC-4 Step 3 & 5)
// ─────────────────────────────────────────────────────────────────────────
/*
  ORDER IS CRITICAL in ASP.NET Core middleware.
  Each request travels DOWN the list; the response travels back UP.

  ExceptionHandling  ←── outermost: catches errors from everything below
       │
  LoggingMiddleware  ←── logs EVERY request (method, path, status, ms)
       │
  Authentication     ←── validates JWT (if route requires it)
       │
  Authorization      ←── checks claims / roles (e.g. role=Admin)
       │
  Ocelot             ←── reverse-proxies to the downstream service
*/

// 1. Exception handler — must be FIRST so it wraps everything
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. Custom logger — logs before forwarding and after receiving response
app.UseMiddleware<LoggingMiddleware>();

// 3. JWT authentication — validates Bearer tokens
app.UseAuthentication();

// 4. Authorization — enforces [Authorize] / route claim requirements
app.UseAuthorization();

// 5. Ocelot — does the actual reverse-proxying based on ocelot.json
//    This MUST be the LAST middleware; nothing runs after it.
await app.UseOcelot();

app.Run();
