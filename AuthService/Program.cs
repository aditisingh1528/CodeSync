using System.Text;
using AuthService.Data;
using AuthService.Repositories;
using AuthService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// CONTROLLERS
builder.Services.AddControllers();

// DATABASE (EF Core + SQL Server)
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuthDB")));

// DEPENDENCY INJECTION
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService,    AuthServiceImpl>();
builder.Services.AddScoped<IJwtService,     JwtService>();

// JWT AUTHENTICATION MIDDLEWARE
var jwtKey      = builder.Configuration["Jwt:Key"]!;
var jwtIssuer   = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtIssuer,
        ValidAudience            = jwtAudience,
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew                = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// SWAGGER with JWT Bearer support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "AuthService API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token. Example: Bearer eyJhbGci..."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// MIDDLEWARE PIPELINE

if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode  = 500;
            context.Response.ContentType = "application/json";

            var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
            var ex = exceptionFeature?.Error;

            await context.Response.WriteAsJsonAsync(new
            {
                message        = ex?.Message ?? "Unknown error",
                innerException = ex?.InnerException?.Message ?? "",
                exceptionType  = ex?.GetType().Name ?? "",
                hint           = GetHint(ex)
            });
        });
    });

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthService v1");
        c.RoutePrefix = string.Empty;
    });
}
else
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode  = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                message = "An unexpected error occurred. Please try again later."
            });
        });
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// AUTO-MIGRATE + STARTUP DB CHECK
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

    try
    {
        db.Database.Migrate();
        Console.WriteLine("✅ Database ready — all migrations applied.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database migration FAILED: {ex.Message}");
        Console.WriteLine($"   Inner: {ex.InnerException?.Message}");
        Console.WriteLine("   → Check SQL Server is running.");
        Console.WriteLine("   → Check 'AuthDB' connection string in appsettings.json.");
        Console.WriteLine("   → Common fix: Server=localhost\\SQLEXPRESS");
    }
}

app.Run();

// Helper: give a specific hint based on the exception type
static string GetHint(Exception? ex)
{
    if (ex == null) return "";

    var msg = ex.Message + (ex.InnerException?.Message ?? "");

    if (msg.Contains("Invalid column name 'Role'"))
        return "The 'Role' column is missing. The auto-migration should fix this on restart. If not, run: dotnet ef database update";

    if (msg.Contains("Cannot open database") || msg.Contains("server was not found"))
        return "SQL Server is not reachable. Check your connection string in appsettings.json.";

    if (msg.Contains("Login failed"))
        return "SQL Server rejected the login. Check the username/password in your connection string.";

    if (msg.Contains("Invalid object name 'Users'"))
        return "The Users table doesn't exist. Run: dotnet ef database update";

    if (msg.Contains("Jwt:Key") || msg.Contains("IDX"))
        return "JWT configuration error. Check Jwt:Key, Jwt:Issuer, Jwt:Audience in appsettings.json.";

    return "Check the server console output for the full stack trace.";
}
