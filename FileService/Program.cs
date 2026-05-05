using System.Text;
using FileService.Data;
using FileService.Repositories;
using FileService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────
// 1. CONTROLLERS
// ─────────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ─────────────────────────────────────────────────────────────────────────
// 2. DATABASE — FileDB (separate from AuthDB and ProjectDB)
// ─────────────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<FileDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("FileDB")));

// ─────────────────────────────────────────────────────────────────────────
// 3. DEPENDENCY INJECTION
// ─────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IFileService,    FileServiceImpl>();

// ─────────────────────────────────────────────────────────────────────────
// 4. REDIS CACHING
//    Same pattern as ProjectService.
//    InstanceName "FileService:" namespaces keys → no collision with ProjectService
// ─────────────────────────────────────────────────────────────────────────
var redisConn = builder.Configuration.GetConnectionString("Redis")!;

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConn;
    options.InstanceName  = "FileService:";
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect($"{redisConn},abortConnect=false"));

builder.Services.AddScoped<ICacheService, RedisCacheService>();

// ─────────────────────────────────────────────────────────────────────────
// 5. JWT — validates tokens issued by AuthService (does NOT issue them)
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
// 6. SWAGGER with JWT Bearer
// ─────────────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FileService API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "Bearer", BearerFormat = "JWT", In = ParameterLocation.Header,
        Description = "Enter JWT from AuthService. Example: Bearer eyJhbGci..."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ─────────────────────────────────────────────────────────────────────────
// 7. MIDDLEWARE
// ─────────────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FileService v1");
        c.RoutePrefix = string.Empty;   // Swagger at http://localhost:5230
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ─────────────────────────────────────────────────────────────────────────
// 8. AUTO-MIGRATE — creates FileDB on startup
// ─────────────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FileDbContext>();
    try
    {
        db.Database.Migrate();
        Console.WriteLine("✅ FileDB ready — all migrations applied.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ FileDB migration FAILED: {ex.Message}");
        Console.WriteLine($"   Inner: {ex.InnerException?.Message}");
        Console.WriteLine("   → Check SQL Server is running and 'FileDB' connection string is correct.");
    }
}

app.Run();
