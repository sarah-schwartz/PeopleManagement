# PeopleManagement

Sample .NET solution for managing a list of people: ASP.NET Core Web API, optional MVC web UI, Entity Framework Core with SQL Server (or LocalDB), and PDF export (QuestPDF).

## Technologies

| Area | Stack |
|------|--------|
| Runtime | .NET 10 |
| API | ASP.NET Core Web API, Swagger (Swashbuckle) |
| Web UI | ASP.NET Core MVC (Razor) |
| Data | Entity Framework Core 9, SQL Server provider |
| Validation | FluentValidation |
| PDF | QuestPDF (Community license) |
| Tests | xUnit, Moq, `WebApplicationFactory`, EF Core SQLite in-memory |

## Architecture

Layers are separated as follows:

- **PeopleManagement.Domain** — `Person` aggregate and domain rules (no infrastructure references).
- **PeopleManagement.Application** — use cases (`IPeopleService`), `CreatePersonInput`, validators, `IPdfExportService` / `IFileStorageService` abstractions.
- **PeopleManagement.Infrastructure** — EF Core `PeopleManagementDbContext`, migrations, `PeopleService`, `LocalFileStorageService`, `QuestPdfPeopleExportService`, middleware.
- **PeopleManagement.Api** — REST API (`/api/People`, …).
- **PeopleManagement.Web** — browser UI (Hebrew), optional alongside the API.

Data flow: **API/Web → `IPeopleService` → `DbContext` / file storage / PDF service**.

## Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **SQL Server** or **SQL Server LocalDB** (for normal local runs with real migrations)
- (Optional) Visual Studio 2022 or VS Code + C# Dev Kit

### Clone and restore

```bash
git clone <repository-url>
cd PeopleManagement
dotnet restore
```

### Connection string

Default LocalDB connection is in `src/PeopleManagement.Web/appsettings.json` and `src/PeopleManagement.Api/appsettings.json` under `ConnectionStrings:DefaultConnection`. Adjust for your SQL instance or use User Secrets:

```bash
cd src/PeopleManagement.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=PeopleManagement;..."
```

### Run the API

```bash
cd src/PeopleManagement.Api
dotnet run
```

Swagger UI (Development): `https://localhost:<port>/swagger`

### Run the Web app

```bash
cd src/PeopleManagement.Web
dotnet run
```

Browse to the HTTPS URL shown in the console (default route: People/Index).

## Database migrations

Migrations live in `src/PeopleManagement.Infrastructure/Persistence/Migrations`.

### Apply migrations (automatic)

On startup, **PeopleManagement.Web** and **PeopleManagement.Api** call `Database.Migrate()` unless `SkipEfMigrations` is set to `true` (used by integration tests).

### Add a new migration

From the repository root (startup project = Api or Web):

```bash
dotnet ef migrations add MigrationName ^
  --project src/PeopleManagement.Infrastructure ^
  --startup-project src/PeopleManagement.Api
```

### Update the database from CLI

```bash
dotnet ef database update ^
  --project src/PeopleManagement.Infrastructure ^
  --startup-project src/PeopleManagement.Api
```

## API endpoints

Base route: `api/People` (controller name `People`).

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/People` | Create a person. `multipart/form-data` fields: `fullName`, `email`, optional `phone`, optional `photo` file field. |
| `GET` | `/api/People` | List all people (JSON array of `Person`). |
| `GET` | `/api/People/{id}` | Get one person by id. |
| `GET` | `/api/People/search?query=` | Search by partial full name (case-insensitive). Empty query returns all. |
| `GET` | `/api/People/export/pdf` | Download PDF of the full list. |

Successful create returns `201 Created` with body `{ "id": <int> }`. Validation errors return `400` with a structured error payload.

## Tests

### Unit tests (`PeopleManagement.UnitTests`)

Focus: `PeopleService` with in-memory EF Core, real `CreatePersonInputValidator`, mocked PDF and file storage.

```bash
dotnet test tests/PeopleManagement.UnitTests/PeopleManagement.UnitTests.csproj
```

### Integration tests (`PeopleManagement.IntegrationTests`)

Uses `WebApplicationFactory` with **EF Core InMemory** and `SkipEfMigrations=true` so tests do not require SQL Server.

```bash
dotnet test tests/PeopleManagement.IntegrationTests/PeopleManagement.IntegrationTests.csproj
```

Run all tests in the solution:

```bash
dotnet test PeopleManagement.sln
```

