# PeopleManagement.Infrastructure

## Purpose

The infrastructure layer — the only project that is allowed to touch the database, the file system, or third-party libraries for PDF generation. It implements every interface declared in `PeopleManagement.Application` and wires them together via a single `AddInfrastructure(configuration)` extension method.

## What it contains

| File | Implements | Role |
|------|-----------|------|
| `Persistence/PeopleManagementDbContext.cs` | — | EF Core `DbContext`; auto-stamps timestamps |
| `Persistence/Configurations/PersonConfiguration.cs` | `IEntityTypeConfiguration<Person>` | Column types, lengths, indexes |
| `Persistence/HostExtensions.cs` | — | `Database.Migrate()` on startup |
| `People/PeopleService.cs` | `IPeopleService` | Orchestrates DB + file storage + PDF |
| `Files/LocalFileStorageService.cs` | `IFileStorageService` | Stores files under `wwwroot/uploads/people/` |
| `Export/QuestPdfPeopleExportService.cs` | `IPdfExportService` | Generates PDFs with QuestPDF |
| `Middleware/GlobalExceptionHandlingMiddleware.cs` | — | Converts unhandled exceptions to JSON or redirect |
| `DependencyInjection.cs` | — | Registers all services with the DI container |

## Key implementation details

### PeopleManagementDbContext — automatic timestamps

`SaveChangesAsync` is overridden to inspect `ChangeTracker` before every save:
- New entities (`EntityState.Added`) → `Person.StampAsCreated(utcNow)`
- Modified entities (`EntityState.Modified`) → `Person.StampAsModified(utcNow)`

This keeps timestamp logic out of every individual call-site; it happens exactly once, transparently.

### PersonConfiguration — database schema

```
Table: People
├── Id             INT IDENTITY PRIMARY KEY
├── FullName       NVARCHAR(200) NOT NULL
├── Email          NVARCHAR(320) NOT NULL
├── Phone          NVARCHAR(20)  NOT NULL  (empty string when absent)
├── ProfilePhotoStoredPath NVARCHAR(512) NULL
├── CreatedAtUtc   DATETIME2 NOT NULL
└── UpdatedAtUtc   DATETIME2 NOT NULL

Unique index: UX_People_Email
```

Column lengths are taken directly from the constants on `Person` (`FullNameMaxLength`, etc.), so the schema can never drift from the domain model.

### PeopleService — use-case orchestration

1. Validates `CreatePersonInput` via FluentValidation (throws `ValidationException` on failure).
2. If a photo is supplied, delegates to `IFileStorageService.SaveAsync()` and stores the returned relative path on the entity.
3. Persists via `DbContext` (timestamps stamped automatically).
4. `SearchAsync` performs a case-insensitive substring match on `FullName`.
5. `ExportPeopleListByIdsAsync` deduplicates IDs, fetches from the DB, and reorders to match the caller's requested order.

### LocalFileStorageService — file storage

- Validates extension and content-type against `PhotoUploadConstraints` (defence-in-depth after the Application-layer validator).
- Generates a GUID-based filename to avoid collisions and path-traversal attacks.
- Returns only the relative path (`uploads/people/{guid}.jpg`); the domain entity never stores an absolute filesystem path.

### QuestPdfPeopleExportService — PDF generation

Uses **QuestPDF 2025.1.0** (Community license). Produces:
- A paginated table listing all people (full name, phone, email) with a blue header row.
- An optional single-person detail sheet.
- Uses Segoe UI to correctly render Hebrew characters.
- Page number in the footer.

### GlobalExceptionHandlingMiddleware

Routes unhandled exceptions based on the request path:
- `/api/*` or `/swagger/*` → returns `{ "message": "..." }` with HTTP 500.
- Everything else → redirects to `/People/Error` for the MVC error page.

## Why this implementation is appropriate

- **EF Core with SQL Server** satisfies the assignment requirement for a relational database with migrations managed by the `dotnet ef` toolchain.
- **Centralised timestamp management in `SaveChanges`** means no controller or service can forget to set `CreatedAtUtc`.
- **GUID filenames** eliminate collisions and prevent directory-traversal attacks without additional escaping logic.
- **QuestPDF** is a well-maintained, production-grade .NET PDF library with a free Community license, an explicit fluent API, and RTL/Unicode font support — a more maintainable choice than iTextSharp or raw Aspose for a project of this scale.
- **`GlobalExceptionHandlingMiddleware`** ensures that unexpected errors never expose stack traces to clients while still returning machine-readable JSON to API consumers.
