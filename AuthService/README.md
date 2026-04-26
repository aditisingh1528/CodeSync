# AuthService — UC-3: JWT Authentication & Role-Based Authorization

## What was added in UC-3

### New files

```
AuthService/
  Services/
    IJwtService.cs          ← interface for JWT token generation
    JwtService.cs           ← implementation using System.IdentityModel.Tokens.Jwt
  Controllers/
    UserController.cs       ← secured endpoints (any logged-in user)
    AdminController.cs      ← admin-only endpoints
  DTOs/
    UserProfileDto.cs       ← profile data read from JWT claims
    UpdateRoleDto.cs        ← body for POST /api/admin/update-role
  Migrations/
    20260423120000_AddRoleToUser.cs   ← adds Role column to Users table

AuthService.Tests/
  JwtServiceTests.cs        ← 9 tests for token generation and validation
  AdminServiceTests.cs      ← 6 tests for role update logic
```

### Modified files (UC-2 → UC-3)

| File | Change |
|------|--------|
| `AuthService.csproj` | Added JwtBearer + System.IdentityModel.Tokens.Jwt packages |
| `appsettings.json` | Added Jwt:Key, Issuer, Audience, ExpiryMinutes |
| `Models/User.cs` | Added `Role` property (default: "User") |
| `Repositories/IUserRepository.cs` | Added `UpdateUserRoleAsync` |
| `Repositories/UserRepository.cs` | Implemented `UpdateUserRoleAsync` |
| `Services/IAuthService.cs` | Added `UpdateUserRoleAsync` |
| `Services/AuthServiceImpl.cs` | Injects IJwtService; generates token on Register + Login |
| `Controllers/AuthController.cs` | Slimmed to public-only routes with [AllowAnonymous] |
| `Program.cs` | Full JWT middleware + Swagger Bearer auth + correct pipeline order |
| `AuthService.Tests.csproj` | Added JWT + IConfiguration packages for testing |
| `AuthServiceImplTests.cs` | Updated to mock IJwtService; added token + role tests |

---

## API Endpoints

### 🔓 Public (no token needed)

| Method | Route | Description |
|--------|-------|-------------|
| GET  | /api/auth/test | Health check |
| POST | /api/auth/register | Register → returns JWT |
| POST | /api/auth/login | Login → returns JWT |

### 🔒 Secured — any logged-in user (valid JWT required)

| Method | Route | Description |
|--------|-------|-------------|
| GET | /api/user/profile | Read profile from JWT claims |
| GET | /api/user/me | Greeting + role info |

### 🔐 Admin only (JWT + Role = "Admin" required)

| Method | Route | Description |
|--------|-------|-------------|
| GET  | /api/admin/dashboard | Admin welcome |
| GET  | /api/admin/users | List all users |
| POST | /api/admin/update-role | Promote or demote a user |

---

## How to use JWT in Swagger

1. Call `POST /api/auth/login` or `POST /api/auth/register`
2. Copy the `token` value from the response
3. Click the **Authorize 🔓** button at the top of Swagger UI
4. Enter: `Bearer <paste-token-here>` and click **Authorize**
5. All secured endpoints now work — Swagger sends the token automatically

---

## How to make a user Admin

### Option A — via Swagger (if you are already an Admin)
```json
POST /api/admin/update-role
{ "userId": 2, "role": "Admin" }
```

### Option B — directly in SQL Server
```sql
UPDATE Users SET Role = 'Admin' WHERE Email = 'youremail@example.com';
```

---

## Running the project

```bash
# 1. Restore packages
dotnet restore

# 2. Apply migrations (adds Role column)
dotnet ef database update

# 3. Run
dotnet run

# 4. Run all tests (37 total across UC-1/2/3)
cd ../AuthService.Tests && dotnet test
```

---

## JWT explained (beginner-friendly)

A JWT (JSON Web Token) looks like: `xxxxx.yyyyy.zzzzz`

- **Header** — says what algorithm was used (HS256)
- **Payload** — contains claims: userId, email, role, expiry (NOT secret — anyone can decode it)
- **Signature** — cryptographic proof that the server created it; cannot be faked without the secret Key

When the client sends `Authorization: Bearer <token>`, the middleware:
1. Splits the token into 3 parts
2. Re-computes the signature using the server's Key
3. If it matches → token is valid → populates `HttpContext.User` with the claims
4. If it doesn't match → 401 Unauthorized

---

## Security notes

- Secret `Jwt:Key` must be at least 32 characters and kept private
- In production, move the key to environment variables or Azure Key Vault — never commit it to git
- Tokens expire after 60 minutes (configurable via `Jwt:ExpiryMinutes`)
- Each token has a unique `jti` claim to support future token revocation
- `ClockSkew = TimeSpan.Zero` means tokens expire exactly on time with no grace period
