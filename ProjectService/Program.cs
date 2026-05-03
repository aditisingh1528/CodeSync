using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProjectService.Data;
using ProjectService.Repositories;
using ProjectService.Services;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────
// 1. CONTROLLERS
// ─────────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ─────────────────────────────────────────────────────────────────────────
// 2. DATABASE — EF Core + SQL Server (ProjectDB)
//    Separate database from AuthDB — microservices own their data
// ─────────────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ProjectDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProjectDB")));

// ─────────────────────────────────────────────────────────────────────────
// 3. DEPENDENCY INJECTION
// ─────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectService,    ProjectServiceImpl>();

// ─────────────────────────────────────────────────────────────────────────
// 4. JWT AUTHENTICATION
//    ProjectService does NOT issue tokens — it only VALIDATES them.
//    The token was issued by AuthService — same Key/Issuer/Audience.
//    If any of these 3 values differ from AuthService → 401 on every call.
// ─────────────────────────────────────────────────────────────────────────
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

// ─────────────────────────────────────────────────────────────────────────
// 5. SWAGGER with JWT Bearer support
// ─────────────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ProjectService API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token from AuthService login. Example: Bearer eyJhbGci..."
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

// ─────────────────────────────────────────────────────────────────────────
// BUILD
// ─────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ─────────────────────────────────────────────────────────────────────────
// 6. MIDDLEWARE PIPELINE
// ─────────────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProjectService v1");
        c.RoutePrefix = string.Empty;   // Swagger at root: http://localhost:5217
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ─────────────────────────────────────────────────────────────────────────
// 7. AUTO-MIGRATE on startup
//    Creates ProjectDB and applies migrations automatically.
//    No need to run "dotnet ef database update" manually.
// ─────────────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
    try
    {
        db.Database.Migrate();
        Console.WriteLine("✅ ProjectDB ready — all migrations applied.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ProjectDB migration FAILED: {ex.Message}");
        Console.WriteLine($"   Inner: {ex.InnerException?.Message}");
        Console.WriteLine("   → Check SQL Server is running.");
        Console.WriteLine("   → Check 'ProjectDB' connection string in appsettings.json.");
    }
}

app.Run();
