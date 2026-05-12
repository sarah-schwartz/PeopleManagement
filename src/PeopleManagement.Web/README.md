# PeopleManagement.Web

## Purpose

An optional browser-facing interface for the same people-management use cases. It provides a Hebrew-language UI for creating people, browsing and searching the list, and exporting selected people to a PDF — all without requiring the user to interact with the REST API directly.

## What it contains

| File | Role |
|------|------|
| `Program.cs` | Startup, localization, migration on boot |
| `Controllers/PeopleController.cs` | MVC actions for all pages |
| `Models/PeopleViewModels.cs` | View models with Hebrew display names and error messages |
| `Models/ErrorViewModel.cs` | Error page model |

## Key flows

### List / Search

`GET /People` (or `GET /People?search=...`) renders an index page. If a search query is present, it calls `IPeopleService.SearchAsync(query)`; otherwise it calls `ListAsync()`. The page includes a form for filtering and checkboxes for selecting people to export.

### Create person

`GET /People/Create` renders the creation form. `POST /People/Create` reads the submitted fields and an optional `IFormFile` photo:
1. Reads the photo stream into a `byte[]` and normalises its content-type from the file extension (browsers sometimes report incorrect MIME types for `.jpg` files).
2. Builds a `CreatePersonInput` and calls `IPeopleService.CreateAsync()`.
3. On success: redirects to the index page with a `TempData` success message.
4. On `ValidationException`: re-renders the form with per-field errors.
5. On duplicate-email `DbUpdateException`: shows a specific "email already in use" error.

### Export PDF

`POST /People/ExportPdf` accepts `selectedIds[]` from the checkboxes on the index page, calls `IPeopleService.ExportPeopleListByIdsAsync(ids)`, and streams the result as a PDF download.

### Error page

`GET /People/Error` renders a generic error view. `GlobalExceptionHandlingMiddleware` redirects non-API errors here with no stack-trace exposure.

## Why this implementation is appropriate

- **Hebrew UI with `RequestLocalizationMiddleware`** — the startup configures `he-IL` as the default culture so that date and number formats are correct for Hebrew-speaking users, and all display names and validation messages are written in Hebrew.
- **Shared service layer** — `PeopleController` (Web) and `PeopleController` (Api) both depend on `IPeopleService`. There is no duplication of business logic between the two entry points.
- **Specific `DbUpdateException` handling** — instead of showing a generic error when a duplicate email is submitted, the controller detects the unique-index violation by inspecting the exception message and surfaces a useful, field-level error message to the user.
- **`NormalizePhotoContentType()`** — a small helper that maps file extensions to canonical MIME types corrects browser inconsistencies (e.g., Chrome sometimes sends `image/jpg` instead of the correct `image/jpeg`) before the data reaches the service layer.
- **`TempData` for cross-redirect messages** — success and error messages survive the POST-Redirect-GET pattern cleanly without storing them in the URL or in the session.

## Suggested improvements

1. **Photo read into `byte[]`** — the controller reads the entire `IFormFile` into memory with `MemoryStream`. For a 5 MB file this is acceptable, but passing the `IFormFile.OpenReadStream()` as a `Stream` all the way to the storage service would eliminate one full copy.
2. **Duplicate-email detection by string matching** — `IsDuplicateEmailViolation()` inspects the exception message text (`"UX_People_Email"`). This is fragile across database providers and SQL Server versions. A better approach is a custom `DomainException` thrown by the service after a pre-check or after catching the DB exception.
3. **No client-side validation** — the form relies entirely on server round-trips for validation. Adding `jquery-validation` (already included in the default ASP.NET Core MVC template) would give users immediate feedback.
4. **Photo upload stores byte[] in `CreatePersonInput`** — see the same note in `PeopleManagement.Api`: both entry points would benefit from a `Stream`-based API on `IFileStorageService`.
