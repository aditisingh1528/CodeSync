# UC-4 — API Gateway with Ocelot in .NET 8

## What Was Built

A standalone **API Gateway** project (`ApiGateway/`) was added to the existing solution.
The gateway sits in front of `AuthService` and every client request **must go through it**.

```
CLIENT  ──►  ApiGateway (port 5000)  ──►  AuthService (port 5160)
              │
              ├─ Validates JWT (if route requires it)
              ├─ Logs every request + response time
              ├─ Catches all unhandled exceptions
              └─ Rate-limits public endpoints
```

---

## Project Structure Added

```
CCP_UC3_applied/
├── ApiGateway/                        ← NEW PROJECT
│   ├── ApiGateway.csproj              ← Ocelot + JwtBearer packages
│   ├── Program.cs                     ← Full bootstrap: services + middleware
│   ├── ocelot.json                    ← All routing rules
│   ├── appsettings.json               ← JWT config (must match AuthService)
│   ├── appsettings.Development.json   ← Dev logging levels
│   ├── Properties/
│   │   └── launchSettings.json        ← Gateway runs on http://localhost:5000
│   └── Middleware/
│       ├── LoggingMiddleware.cs        ← Logs every request/response
│       └── ExceptionHandlingMiddleware.cs  ← Converts exceptions to JSON
│
├── AuthService/                       ← UNCHANGED (runs on port 5160)
├── AuthService.Tests/                 ← UNCHANGED
└── CCP.sln                            ← Updated to include ApiGateway
```

---

## Step-by-Step Explanation

### Step 1 — Add Ocelot Package

**File:** `ApiGateway/ApiGateway.csproj`

```xml
<PackageReference Include="Ocelot" Version="23.4.2" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
```

**What it does:**  
`Ocelot` is a .NET API Gateway library. It reads a JSON config file (`ocelot.json`) and acts as a reverse proxy — it receives a request from the client, rewrites the URL, and forwards it to the real downstream service.

To register it in `Program.cs`:
```csharp
builder.Services.AddOcelot(builder.Configuration);
// ...
await app.UseOcelot();  // must be LAST in the pipeline
```

---

### Step 2 — Configure Routing for AuthService

**File:** `ApiGateway/ocelot.json`

Ocelot routing works by matching `UpstreamPathTemplate` (what the client sends to the gateway) and rewriting it to `DownstreamPathTemplate` (what gets sent to AuthService).

#### Route 1 — Public auth endpoints (no JWT)
```
Client request:   POST http://localhost:5000/auth/login
Gateway forwards: POST http://localhost:5160/api/auth/login
```

```json
{
  "UpstreamPathTemplate":   "/auth/{everything}",
  "DownstreamPathTemplate": "/api/auth/{everything}",
  "DownstreamHostAndPorts": [{ "Host": "localhost", "Port": 5160 }]
}
```

#### Route 2 — Secured user endpoints (JWT required)
```
Client request:   GET http://localhost:5000/user/profile
Gateway validates JWT, then forwards:
                  GET http://localhost:5160/api/user/profile
```

#### Route 3 — Admin endpoints (JWT + role=Admin)
```
Client request:   GET http://localhost:5000/admin/users
Gateway validates JWT AND checks role=Admin, then forwards:
                  GET http://localhost:5160/api/admin/users
```

The `{everything}` wildcard catches any sub-path, so `/auth/register`, `/auth/login`, and `/auth/test` all match the same rule.

---

### Step 3a — Logging Middleware

**File:** `ApiGateway/Middleware/LoggingMiddleware.cs`

```csharp
public async Task InvokeAsync(HttpContext context)
{
    var startTime = DateTime.UtcNow;

    _logger.LogInformation("[Gateway ▶ REQUEST]  {Method} {Path}  |  {Time}", ...);

    await _next(context);  // forward to next middleware

    var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
    _logger.LogInformation("[Gateway ◀ RESPONSE] {Method} {Path}  |  Status: {Status}  |  {Elapsed}ms", ...);
}
```

**What you see in the terminal:**
```
[Gateway ▶ REQUEST]  POST /auth/login  |  2026-04-24 10:30:01.123
[Gateway ◀ RESPONSE] POST /auth/login  |  Status: 200  |  Elapsed: 42.3ms
```

The middleware wraps `_next(context)` so it logs **before** forwarding (captures the path the client sent) and **after** the downstream response returns (captures status code and elapsed time).

---

### Step 3b — Exception Handling Middleware

**File:** `ApiGateway/Middleware/ExceptionHandlingMiddleware.cs`

```csharp
public async Task InvokeAsync(HttpContext context)
{
    try   { await _next(context); }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[Gateway ❌ EXCEPTION] ...");
        await WriteErrorResponseAsync(context, ex);
    }
}
```

**In Development** — full detail is returned:
```json
{
  "statusCode": 500,
  "message": "An unhandled exception occurred in the gateway.",
  "exceptionType": "InvalidOperationException",
  "detail": "...",
  "inner": "..."
}
```

**In Production** — safe generic message:
```json
{ "statusCode": 500, "message": "An unexpected error occurred. Please try again later." }
```

This middleware is placed **first** in the pipeline so it wraps everything — any exception anywhere (including inside Ocelot) is caught here.

---

### Step 4 — JWT Authentication in the Gateway

**File:** `ApiGateway/Program.cs`

```csharp
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
            ValidIssuer              = "AuthService",
            ValidAudience            = "AuthServiceClients",
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew                = TimeSpan.Zero
        };
    });
```

The scheme name `"GatewayJwt"` is referenced in `ocelot.json`:
```json
"AuthenticationOptions": {
  "AuthenticationProviderKey": "GatewayJwt"
}
```

**Flow for a secured route:**
1. Client sends `Authorization: Bearer <token>` header.
2. `UseAuthentication()` validates the token using the `"GatewayJwt"` scheme.
3. If valid → `UseAuthorization()` checks any claim requirements (e.g., `role=Admin`).
4. If all pass → Ocelot forwards the request (with the original `Authorization` header) to AuthService.
5. If token is missing/expired → gateway returns `401 Unauthorized` immediately. AuthService is never called.

> **Key:** The `Jwt:Key`, `Jwt:Issuer`, and `Jwt:Audience` in `ApiGateway/appsettings.json` are identical to those in `AuthService/appsettings.json`. A token issued by AuthService's login endpoint will validate successfully at the gateway.

---

### Step 5 — Middleware Pipeline Order

**File:** `ApiGateway/Program.cs`

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();  // 1. outermost — catches all errors
app.UseMiddleware<LoggingMiddleware>();            // 2. logs every request/response
app.UseAuthentication();                          // 3. validates JWT
app.UseAuthorization();                           // 4. checks claims/roles
await app.UseOcelot();                            // 5. reverse-proxy (MUST BE LAST)
```

Order matters because ASP.NET Core middleware forms a chain. Each item calls the next one. If you put `UseOcelot()` before `UseAuthentication()`, the JWT would never be validated.

---

## How to Run

### Prerequisites
- .NET 8 SDK
- SQL Server (already used by AuthService in UC-3)
- AuthService running (the gateway forwards to it)

### Terminal 1 — Start AuthService

```bash
cd CCP_UC3_applied/AuthService
dotnet run
```

AuthService will start on: `http://localhost:5160`
You should see: `✅ Database ready — all migrations applied.`

### Terminal 2 — Start ApiGateway

```bash
cd CCP_UC3_applied/ApiGateway
dotnet run
```

Gateway will start on: `http://localhost:5000`

> **From now on, all API calls go to port 5000, not 5160.**

---

## Postman Testing (UC-4 Step 6)

### Route: `/auth/*`  — Public, no token needed

#### 1. Health Check
```
GET http://localhost:5000/auth/test
```
Expected: `200 OK` with `{ "message": "AuthService is running." }`

#### 2. Register
```
POST http://localhost:5000/auth/register
Content-Type: application/json

{
  "username": "john_doe",
  "email":    "john@example.com",
  "password": "Password@123"
}
```
Expected: `201 Created` with JWT token in `user.token`.

#### 3. Login
```
POST http://localhost:5000/auth/login
Content-Type: application/json

{
  "email":    "john@example.com",
  "password": "Password@123"
}
```
Expected: `200 OK` with JWT token. **Copy this token for the next calls.**

---

### Route: `/user/*`  — Requires valid JWT

#### 4. Get Profile (with token)
```
GET http://localhost:5000/user/profile
Authorization: Bearer <paste-token-here>
```
Expected: `200 OK` with userId, username, email, role.

#### 5. Get Profile (no token — gateway blocks it)
```
GET http://localhost:5000/user/profile
```
Expected: `401 Unauthorized` — returned by the **gateway**, AuthService is never called.

---

### Route: `/admin/*`  — Requires JWT with role=Admin

#### 6. List Users as Admin
```
GET http://localhost:5000/admin/users
Authorization: Bearer <admin-token>
```
Expected: `200 OK` with user list (if token has role=Admin).

#### 7. Non-admin trying admin route
```
GET http://localhost:5000/admin/users
Authorization: Bearer <regular-user-token>
```
Expected: `403 Forbidden` — gateway rejects based on claim check.

---

## Verifying the Gateway Logs

While running Postman tests, watch Terminal 2 (the gateway terminal). You will see:

```
[Gateway ▶ REQUEST]  POST /auth/login  |  2026-04-24 10:30:01.123
[Gateway ◀ RESPONSE] POST /auth/login  |  Status: 200  |  Elapsed: 43.7ms

[Gateway ▶ REQUEST]  GET /user/profile  |  2026-04-24 10:30:05.456
[Gateway ◀ RESPONSE] GET /user/profile  |  Status: 200  |  Elapsed: 12.1ms

[Gateway ▶ REQUEST]  GET /user/profile  |  2026-04-24 10:30:09.789
[Gateway ◀ RESPONSE] GET /user/profile  |  Status: 401  |  Elapsed: 1.2ms
```

The `401` response has only 1.2ms elapsed — proof the gateway rejected it immediately without calling AuthService.

---

## Architecture Summary

```
                    ┌─────────────────────────────────────────────┐
                    │             API GATEWAY  :5000               │
                    │                                              │
  CLIENT ──────────►│  ExceptionHandlingMiddleware (outermost)    │
                    │         │                                    │
                    │  LoggingMiddleware (log req + res + ms)      │
                    │         │                                    │
                    │  UseAuthentication (validate JWT)            │
                    │         │                                    │
                    │  UseAuthorization (check role claims)        │
                    │         │                                    │
                    │  UseOcelot ──────────────────────────────────┼──► AuthService :5160
                    │    routes:                                   │       /api/auth/*
                    │    /auth/*    → /api/auth/*    (public)      │       /api/user/*
                    │    /user/*    → /api/user/*    (JWT req)     │       /api/admin/*
                    │    /admin/*   → /api/admin/*   (JWT+Admin)   │
                    └─────────────────────────────────────────────┘
```
