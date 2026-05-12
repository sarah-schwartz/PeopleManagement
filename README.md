# PeopleManagement

A .NET 10 Web API for managing a list of people: create, list, search, and export to PDF.

## Technologies

| Area | Stack |
|------|--------|
| Runtime | .NET 10 |
| API | ASP.NET Core Web API, Swagger (Swashbuckle) |
| Web UI | ASP.NET Core MVC (Razor) |
| Data | Entity Framework Core 10, SQL Server |
| Validation | FluentValidation |
| PDF | QuestPDF (Community license) |
| Tests | xUnit, Moq, `WebApplicationFactory`, EF Core SQLite in-memory |

## Architecture

- **PeopleManagement.Domain** — `Person` entity with no external dependencies.
- **PeopleManagement.Application** — use cases (`IPeopleService`), `CreatePersonInput`, validators, abstractions.
- **PeopleManagement.Infrastructure** — EF Core `PeopleManagementDbContext`, migrations, `PeopleService`, `LocalFileStorageService`, `QuestPdfPeopleExportService`, middleware.
- **PeopleManagement.Api** — REST API (`/api/People`).
- **PeopleManagement.Web** — browser UI.

Data flow: **API/Web → `IPeopleService` → `DbContext` / file storage / PDF service**.

## Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **SQL Server** (SQL Server Express with LocalDB is sufficient for local development)

### Clone and restore

```bash
git clone <repository-url>
cd PeopleManagement
dotnet restore
```

### Connection string

The default connection string is in `src/PeopleManagement.Api/appsettings.json` under `ConnectionStrings:DefaultConnection`. To override it, edit the file directly or use User Secrets:

```bash
cd src/PeopleManagement.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=PeopleManagement;Trusted_Connection=True;TrustServerCertificate=true"
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

## Database migrations

Migrations are located in `src/PeopleManagement.Infrastructure/Migrations`.

### Apply migrations (automatic)

On startup, **PeopleManagement.Api** and **PeopleManagement.Web** call `Database.Migrate()` automatically — tables are created if they do not exist.

### Add a new migration

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

Base route: `/api/People`

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/People` | Create a person. `multipart/form-data` fields: `fullName`, `email`, optional `phone`, optional `photo` file. |
| `GET` | `/api/People` | List all people (JSON array). |
| `GET` | `/api/People/{id}` | Get one person by id. |
| `GET` | `/api/People/search?query=` | Search by partial name (case-insensitive). Empty query returns all. |
| `GET` | `/api/People/export/pdf` | Download a PDF of the full list. |

A successful create returns `201 Created` with body `{ "id": <int> }`. Validation errors return `400` with a structured error payload.

## Tests

### Unit tests

```bash
dotnet test tests/PeopleManagement.UnitTests/PeopleManagement.UnitTests.csproj
```

### Integration tests

Use `WebApplicationFactory` with SQLite in-memory — no SQL Server required.

```bash
dotnet test tests/PeopleManagement.IntegrationTests/PeopleManagement.IntegrationTests.csproj
```

### All tests

```bash
dotnet test PeopleManagement.sln
```

## Screenshots

### Home screen — people list (empty state)
![Home screen](<pictures for readme/מסך הבית.png>)

### Add a new person — form with full name, email, phone, and optional profile photo
![Add person form](<pictures for readme/הוספת אדם.png>)

### Person added successfully — success banner and updated list
![Person added successfully](<pictures for readme/אדם נוסף בהצלחה.png>)

### Search by name — filtered results in real time
![Search by name](<pictures for readme/חיפוש לפי שם.png>)

### PDF export — people list exported as a formatted PDF
![PDF export](<pictures for readme/PDF עם רשימת אנשים.png>)

### All 17 tests passing — unit and integration
![Tests passing](<pictures for readme/טסטסים רצים.png>)
