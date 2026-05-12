# PeopleManagement.Api

## Purpose

The REST API entry point. Exposes the people-management use cases over HTTP/JSON so that any client (web app, mobile app, Postman, automated tests) can create, list, search, and export people without coupling to a specific UI technology.

## Endpoints

Base route: `/api/People`

| Method | Path | Request | Success response |
|--------|------|---------|-----------------|
| `POST` | `/api/People` | JSON `CreatePersonInput` body | `201 Created` `{ "id": <int> }` |
| `GET` | `/api/People` | — | `200 OK` `Person[]` |
| `GET` | `/api/People/{id}` | — | `200 OK` `Person` or `404 Not Found` |
| `GET` | `/api/People/search?query=` | Query string | `200 OK` `Person[]` |
| `GET` | `/api/People/export/pdf` | — | `200 OK` `application/pdf` download |

Validation errors return `400 Bad Request` with a structured payload:

```json
{
  "errors": {
    "Email": ["'Email' is not a valid email address."],
    "FullName": ["'Full Name' must not be empty."]
  }
}
```

## How it is implemented

### Program.cs — startup

1. Calls `AddApplication()` — registers FluentValidation validators.
2. Calls `AddInfrastructure(configuration)` — registers DbContext, `PeopleService`, `LocalFileStorageService`, `QuestPdfPeopleExportService`.
3. Registers Swagger / Swashbuckle for OpenAPI documentation (Development environment only).
4. Applies EF Core migrations on startup via `ApplyPeopleManagementMigrations()` unless the `SkipEfMigrations` configuration key is set to `"true"` (used by integration tests).
5. Adds `GlobalExceptionHandlingMiddleware` to convert unhandled exceptions to JSON 500 responses.

### PeopleController

A standard `[ApiController]` with attribute routing (`[Route("api/[controller]")]`). Each action:
- Delegates to `IPeopleService` (injected via constructor).
- Catches `ValidationException` from FluentValidation and converts it to a grouped `400` response.
- Returns typed `ActionResult<T>` with explicit `[ProducesResponseType]` annotations so Swagger shows the correct response schemas.

The controller contains no business logic — it translates HTTP verbs and JSON bodies into service calls and service results back into HTTP responses.

### Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Swashbuckle.AspNetCore` | 7.2.0 | Swagger / OpenAPI UI |
| `PeopleManagement.Application` | project ref | Interfaces and DTOs |
| `PeopleManagement.Infrastructure` | project ref | Concrete service registrations |

## Why this implementation is appropriate

- **`[ApiController]`** enables automatic model-binding validation, `ProblemDetails` responses, and removes the need for `if (!ModelState.IsValid)` boilerplate.
- **Thin controller** — all logic lives in `PeopleService`. The controller's only responsibilities are HTTP translation and error mapping, which keeps it easy to read and test.
- **Swagger** provides immediate interactive documentation without any additional tooling — useful for both manual testing and communicating the API surface to other developers.
- **`SkipEfMigrations` flag** makes the project usable as a `WebApplicationFactory` host in integration tests without a live SQL Server instance.

