# AuthService — UC-2: Register & Login

## What was added in UC-2

### New folders / files

```
AuthService/
  DTOs/
    RegisterDto.cs       ← input model for POST /api/auth/register
    LoginDto.cs          ← input model for POST /api/auth/login
    AuthResponseDto.cs   ← safe response (no PasswordHash exposed)

AuthService.Tests/
  AuthService.Tests.csproj
  DtoValidationTests.cs       ← 10 tests for DTO annotation rules
  AuthServiceImplTests.cs     ← 7 tests for service business logic
```

### Modified files (UC-1 → UC-2)

| File | What changed |
|------|-------------|
| `AuthService.csproj` | Added BCrypt.Net-Next 4.0.3 |
| `Repositories/IUserRepository.cs` | Added GetUserByEmailAsync, EmailExistsAsync, UsernameExistsAsync |
| `Repositories/UserRepository.cs` | Implemented the 3 new repository methods |
| `Services/IAuthService.cs` | Added RegisterAsync and LoginAsync signatures |
| `Services/AuthServiceImpl.cs` | Full register + login with BCrypt |
| `Controllers/AuthController.cs` | Added POST /register and POST /login endpoints |
| `AuthService.sln` | Added AuthService.Tests project |

---

## API Endpoints

### GET /api/auth/test
Health check.

### POST /api/auth/register
Body: { "username": "alice", "email": "alice@example.com", "password": "secret123" }
- 201 Created  — registration OK
- 400 Bad Request — validation failed
- 409 Conflict — email or username already taken

### POST /api/auth/login
Body: { "email": "alice@example.com", "password": "secret123" }
- 200 OK — login OK
- 400 Bad Request — validation failed
- 401 Unauthorized — wrong credentials

---

## Running

```bash
# Run the API
cd AuthService && dotnet run

# First-time DB setup
dotnet ef migrations add InitialCreate
dotnet ef database update

# Run tests
cd AuthService.Tests && dotnet test
```

---

## UC-1 Bugs Fixed

| Bug | Fix |
|-----|-----|
| PasswordHash exposed to client | Use AuthResponseDto instead of raw User model |
| No password hashing | BCrypt.HashPassword on register, BCrypt.Verify on login |
| [Required] did not catch empty strings | Changed to [Required(AllowEmptyStrings = false)] |
| Missing repository methods | Added GetUserByEmailAsync, EmailExistsAsync, UsernameExistsAsync |

---

## Security Notes

- Passwords are ALWAYS hashed with BCrypt before saving — never stored plain-text.
- Login error message is generic ("Invalid email or password") to prevent user enumeration.
- AuthResponseDto is always returned to the client — the raw User model (with PasswordHash) never leaves the service layer.
