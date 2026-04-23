# Code Collaboration Platform — UC-1: AuthService

## Project Structure

```
AuthService/
├── Controllers/
│   └── AuthController.cs       → handles HTTP requests
├── Services/
│   ├── IAuthService.cs         → service interface
│   └── AuthServiceImpl.cs      → business logic
├── Repositories/
│   ├── IUserRepository.cs      → repository interface
│   └── UserRepository.cs       → EF Core DB operations
├── Models/
│   └── User.cs                 → User entity (maps to DB table)
├── Data/
│   └── AuthDbContext.cs        → EF Core DbContext
├── Program.cs                  → app wiring (DI, middleware, Swagger)
└── appsettings.json            → config + connection string
```

---

## Prerequisites

- .NET 8 SDK
- SQL Server (local or SQL Express)
- EF Core CLI tools

---

## Step 1 — Install EF Core CLI (once)

```bash
dotnet tool install --global dotnet-ef
```

---

## Step 2 — Update Connection String

Open `appsettings.json` and update this line:

```json
"AuthDB": "Server=YOUR_SERVER;Database=AuthDB;Trusted_Connection=True;TrustServerCertificate=True;"
```

Common values:
- Local SQL Server → `Server=localhost`
- SQL Express      → `Server=localhost\SQLEXPRESS`

---

## Step 3 — Restore Packages

```bash
cd AuthService
dotnet restore
```

---

## Step 4 — Add Migration

```bash
dotnet ef migrations add InitialCreate
```

This generates a `Migrations/` folder with the schema for the `Users` table.

---

## Step 5 — Update Database

```bash
dotnet ef database update
```

This creates the `AuthDB` database and applies the migration.

---

## Step 6 — Run the Service

```bash
dotnet run
```

---

## Step 7 — Test

| What | URL |
|------|-----|
| Swagger UI | `http://localhost:5000` |
| Test endpoint | `GET http://localhost:5000/api/auth/test` |
| Users list | `GET http://localhost:5000/api/auth/users` |

Expected response from `/api/auth/test`:
```json
{
  "message": "AuthService is running!",
  "timestamp": "2025-01-01T00:00:00Z"
}
```

---

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/auth/test` | Health check — confirms service is up |
| GET | `/api/auth/users` | Returns all users from AuthDB |
