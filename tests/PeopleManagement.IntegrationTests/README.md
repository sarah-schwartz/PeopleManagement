# PeopleManagement.IntegrationTests

## Purpose

Verifies that the full HTTP pipeline — routing → controller → service → database — works end-to-end as a single system. Unlike unit tests, integration tests catch wiring mistakes: a misconfigured route, a missing DI registration, or a broken middleware order.

## What is tested

| Test class | Tests |
|-----------|-------|
| `ApiSmokeTests` | Swagger JSON document returns 200; `GET /api/People` returns 200 |
| `PeopleApiIntegrationTests` | Create a person then confirm they appear in `GET /api/People`; search returns only matching people; creating with an invalid email returns `400` with a structured error body |

## How it is implemented

### ApiWebApplicationFactory

A `WebApplicationFactory<Program>` subclass that reconfigures the API host for testing:

1. **Environment** — sets `ASPNETCORE_ENVIRONMENT=Development` so Swagger is enabled.
2. **Skip migrations** — injects `SkipEfMigrations=true` so `Database.Migrate()` is not called; tests do not need a SQL Server instance.
3. **Swap DbContext** — removes the SQL Server registration and replaces it with `UseInMemoryDatabase(Guid.NewGuid().ToString())`. Each factory instance gets a completely isolated in-memory database.
4. **Swap file storage** — replaces `IFileStorageService` with `TestFileStorageService`, a no-op stub that returns a dummy path without touching the file system.

### TestFileStorageService

```csharp
SaveAsync   → returns "uploads/people/test-photo.jpg"
OpenReadAsync → returns null
DeleteAsync → no-op
```

Keeps tests hermetic: no files are written or read during the test run.

### Toolset

| Package | Version | Role |
|---------|---------|------|
| `Microsoft.AspNetCore.Mvc.Testing` | 9.0.11 | `WebApplicationFactory` host |
| `Microsoft.AspNetCore.TestHost` | 9.0.11 | In-process test server |
| `Microsoft.EntityFrameworkCore.InMemory` | 9.0.11 | Database for tests |
| `xUnit` | 2.9.2 | Test runner |
| `coverlet.collector` | 6.0.2 | Coverage |

## Why this implementation is appropriate

- **`WebApplicationFactory`** spins up the real ASP.NET Core pipeline (middleware, routing, DI container) in-process, giving confidence that the production configuration works — without opening a network port or requiring a running server.
- **InMemory DB per factory instance** provides perfect isolation: tests cannot pollute each other's data even when running in parallel.
- **`SkipEfMigrations` flag** makes the approach viable in any CI environment (GitHub Actions, Azure Pipelines, etc.) without a SQL Server service container.
- **`TestFileStorageService` stub** keeps the test suite free of disk I/O while still exercising the full create-person flow (validation → service → DB → response).

## Run the tests

```bash
dotnet test tests/PeopleManagement.IntegrationTests
```

## Suggested improvements

1. **EF Core InMemory does not enforce unique constraints**, so the duplicate-email scenario is not testable in integration tests either. SQLite in-memory mode would allow a test that POSTs the same email twice and asserts a `409 Conflict` or `400 Bad Request` response.
2. **Photo upload is not integration-tested.** Adding a test that POSTs `multipart/form-data` with a small image file (or a JSON payload with `photoContent`) and asserts the person is created with a non-null `profilePhotoStoredPath` would complete the coverage of the create flow.
3. **A single shared `WebApplicationFactory` instance per test class** via `IClassFixture<ApiWebApplicationFactory>` would avoid recreating the host for every test — reducing test suite startup time significantly once more tests are added.
4. **No test for `GET /api/People/export/pdf`.** Adding a smoke-level test that calls the endpoint and asserts `Content-Type: application/pdf` and a non-empty response body would confirm the QuestPDF integration works end-to-end.
