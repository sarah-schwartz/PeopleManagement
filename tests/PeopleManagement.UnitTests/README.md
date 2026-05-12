# PeopleManagement.UnitTests

## Purpose

Verifies the core business behaviour of `PeopleService` in complete isolation from HTTP, SQL Server, and the file system. Tests run in milliseconds with no external dependencies.

## What is tested

| Test | What it verifies |
|------|-----------------|
| `ListAsync_returns_empty_when_database_is_empty` | Service returns an empty list when the DB has no rows |
| `CreateAsync_persists_person_and_returns_id` | Created person is saved; returned ID matches the DB record; strings are trimmed |
| `CreateAsync_with_photo_calls_file_storage` | When photo fields are provided, `IFileStorageService.SaveAsync` is called with the correct content, filename, and content-type |
| `CreateAsync_invalid_email_throws_ValidationException` | Bad email triggers FluentValidation before any DB write |
| `GetByIdAsync_returns_null_when_missing` | Returns `null` for an unknown ID (no exception) |
| `SearchAsync_empty_query_behaves_like_list_all` | An empty/whitespace query returns all people |
| `SearchAsync_filters_by_full_name_case_insensitive` | Partial, case-insensitive name match returns only matching records |
| `ExportPeopleListAsync_delegates_to_pdf_service` | Service fetches people from DB and passes them to `IPdfExportService` |
| `ExportPeopleListByIdsAsync_preserves_order_and_skips_unknown_ids` | ID ordering is preserved and unknown IDs are silently ignored |
| _(DI smoke test)_ | `AddApplication()` can resolve `CreatePersonInputValidator` from the container |

## How it is implemented

### Toolset

| Package | Version | Role |
|---------|---------|------|
| `xUnit` | 2.9.2 | Test runner and assertions |
| `Moq` | 4.20.72 | Mock `IPdfExportService` and `IFileStorageService` |
| `Microsoft.EntityFrameworkCore.InMemory` | 9.0.11 | In-process DB (no SQL Server needed) |
| `coverlet.collector` | 6.0.2 | Code-coverage collection |

### Test setup

Each test class creates a fresh `PeopleManagementDbContext` backed by an in-memory database with a unique name (via `Guid.NewGuid()`), preventing state leaking between tests. `IPdfExportService` and `IFileStorageService` are mocked with Moq so their behaviour can be controlled and verified per test.

The **real** `CreatePersonInputValidator` is used (not mocked), so validation tests exercise the actual FluentValidation rules rather than a stub.

## Why this implementation is appropriate

- **Moq** isolates external I/O (file system, PDF renderer) so tests are deterministic and instant.
- **EF Core InMemory** eliminates the need for a live SQL Server instance in CI while still exercising LINQ queries through the real DbContext.
- **One DB per test** (GUID-named) guarantees full isolation between test cases without needing `IClassFixture` cleanup logic.
- **Real validator** — using the actual `CreatePersonInputValidator` instead of a mock means validation-rule changes will immediately break the relevant test, catching regressions early.

## Run the tests

```bash
dotnet test tests/PeopleManagement.UnitTests
```

## Suggested improvements

1. **EF Core InMemory does not enforce unique constraints.** The `UX_People_Email` unique index defined in `PersonConfiguration` is silently ignored by the in-memory provider. The duplicate-email path (`DbUpdateException`) cannot be unit-tested here. Use **SQLite in-memory mode** (`UseSqlite("Data Source=:memory:")`) which does honour unique constraints and is still fast and dependency-free.
2. **No test for the `ExportPeopleListByIdsAsync` ordering guarantee beyond one scenario.** Adding parameterised tests (`[Theory]` with `[InlineData]`) would cover edge cases like all-unknown IDs, a single ID, and reversed order.
3. **Moq `Mock<IFileStorageService>` returns `""` by default for `SaveAsync`.** A more realistic stub that returns a well-formed path (`"uploads/people/test.jpg"`) would catch any code that blindly trusts the path without validation.
