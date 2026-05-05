using System.Text;
using ApiGateway.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

// BUILD THE HOST
var builder = WebApplication.CreateBuilder(args);

builder.Configuration
       .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// JWT AUTHENTICATION

var jwtKey      = builder.Configuration["Jwt:Key"]!;
var jwtIssuer   = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)

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
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ADD OCELOT 
builder.Services.AddOcelot(builder.Configuration);

// LOGGING
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// BUILD THE APP
var app = builder.Build();

//MIDDLEWARE PIPELINE ORDER 

// Exception handler
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Custom logger 
app.UseMiddleware<LoggingMiddleware>();

// JWT authentication
app.UseAuthentication();

// Authorization 
app.UseAuthorization();

// Ocelot 
await app.UseOcelot();

app.Run();
