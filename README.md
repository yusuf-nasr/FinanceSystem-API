# Finance System API (.NET)

A RESTful financial transaction management API built with **ASP.NET Core** (.NET 10) and **PostgreSQL**. It provides full support for transactions, document management, budget categories, and role-based access control.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 10 (Web API) |
| ORM | Entity Framework Core |
| Database | PostgreSQL |
| Auth | JWT Bearer tokens (access + refresh) |
| API Docs | Swagger / OpenAPI |
| Password Hashing | BCrypt.Net |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL](https://www.postgresql.org/download/) (running locally or remotely)
- [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) — install once with:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

---

## Configuration

All configuration lives in `appsettings.json`. Before running, update the values to match your environment:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=FinanceDB;Username=postgres;Password=yourpassword"
  },
  "Jwt": {
    "Issuer": "https://localhost:5146",
    "Audience": "https://localhost:5146",
    "Key": "YourSuperSecretKeyThatMustBeAtLeast64BytesLong!!"
  }
}
```

> **Important:** The `Jwt.Key` must be **at least 64 characters** long.

---

## How to Run

### 1. Clone the repository

```bash
git clone <repository-url>
cd FinanceSystem-Dotnet
```

### 2. Restore dependencies

```bash
dotnet restore
```

### 3. Apply database migrations

Make sure PostgreSQL is running and the connection string in `appsettings.json` is correct, then run:

```bash
dotnet ef database update
```

This will create the database and all required tables automatically.

### 4. Run the application

```bash
dotnet run
```

The API will start at:
- **HTTP:** `http://localhost:5146`
- **HTTPS:** `https://localhost:7080`

### 5. Explore the API with Swagger

Open your browser and navigate to:

```
http://localhost:5146/swagger
```

Use the **Authorize** button in Swagger UI to paste your JWT token and test protected endpoints.

---

## Authentication

### Login

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "name": "admin",
  "password": "yourpassword"
}
```

Response:
```json
{
  "access_token": "<JWT>",
  "refresh_token": "<JWT>",
  "user": { ... }
}
```

### Refresh token

```http
POST /api/v1/auth/refresh
Content-Type: application/json

{
  "refreshToken": "<refresh_token>"
}
```

---

## API Endpoints Overview

| Module | Base Route | Notes |
|--------|-----------|-------|
| Auth | `/api/v1/auth` | Login, refresh |
| Users | `/api/v1/users` | CRUD — Admin manages all; users can update themselves |
| Departments | `/api/v1/departments` | Admin only |
| Transaction Types | `/api/v1/transaction-types` | CRUD |
| Transactions | `/api/v1/transactions` | Role-based access, inbox/outgoing/archive filters |
| Transaction Forwards | `/api/v1/transactions/:id/forwards` | Forward, respond, update |
| Documents | `/api/v1/documents` | Upload PDF, download, delete |
| Budget Categories | `/api/v1/budget-categories` | Admin only — CRUD + entries |

---

## Roles

| Role | Permissions |
|------|------------|
| `ADMIN` | Full access to everything |
| `ACCOUNTANT` | Can set `fulfilled`, `budgetName`, `budgetAllocation` on transactions |
| `USER` | Can create/view/forward their own transactions, upload documents |

---

## Running Migrations (manual)

To create a new migration after model changes:

```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

To revert the last migration:

```bash
dotnet ef database update <PreviousMigrationName>
dotnet ef migrations remove
```

---

## Project Structure

```
FinanceSystem-Dotnet/
├── Controllers/          # API endpoints
├── DAL/                  # DbContext (EF Core)
├── DTOs/                 # Request/response data transfer objects
├── Enums/                # Roles, error codes, status enums
├── Exceptions/           # ApiException and global error handling
├── Migrations/           # EF Core migration history
├── Models/               # Entity models
├── Services/             # Business logic (interfaces + implementations)
├── appsettings.json      # App configuration
└── Program.cs            # App startup and DI registration
```
