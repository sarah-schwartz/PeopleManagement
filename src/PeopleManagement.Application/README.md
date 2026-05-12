# PeopleManagement.Application

## Purpose

The use-case layer. It orchestrates what the system can do (create a person, list people, search, export to PDF) without knowing how those actions are carried out physically. All infrastructure concerns (database, files, PDF rendering) are hidden behind interfaces defined here.

## What it contains

| File | Role |
|------|------|
| `People/IPeopleService.cs` | Main service contract consumed by API and Web controllers |
| `People/CreatePersonInput.cs` | Input DTO for person creation |
| `People/Validators/CreatePersonInputValidator.cs` | FluentValidation rules |
| `People/Validation/PhotoUploadConstraints.cs` | Centralised photo-upload limits |
| `Export/IPdfExportService.cs` | Abstraction for PDF generation |
| `Files/IFileStorageService.cs` | Abstraction for file persistence |
| `DependencyInjection.cs` | `AddApplication()` extension method |

### IPeopleService — the contract

```csharp
Task<int>          CreateAsync(CreatePersonInput input, CancellationToken ct);
Task<IReadOnlyList<Person>> ListAsync(CancellationToken ct);
Task<Person?>      GetByIdAsync(int id, CancellationToken ct);
Task<IReadOnlyList<Person>> SearchAsync(string query, CancellationToken ct);
Task<byte[]>       ExportPeopleListAsync(CancellationToken ct);
Task<byte[]>       ExportPeopleListByIdsAsync(IReadOnlyList<int> ids, CancellationToken ct);
```

### CreatePersonInput — the input DTO

Carries the data from an HTTP request to the service: `FullName`, `Email`, `Phone` (optional), and an optional photo represented as `PhotoContent` (byte[]), `PhotoFileName`, and `PhotoContentType`.

### CreatePersonInputValidator — validation rules

Built with **FluentValidation 11**. Rules:

| Field | Rules |
|-------|-------|
| FullName | Not empty; max 200 chars |
| Email | Not empty; valid format; max 320 chars |
| Phone | Optional; max 20 chars |
| Photo | All three photo fields must be supplied together (or all omitted); max 5 MB; extensions: `.jpg .jpeg .png .webp`; content types: `image/jpeg image/png image/webp` |

Error messages are written in Hebrew to match the project's UI language.

### PhotoUploadConstraints — single source of truth

`MaxBytes`, `AllowedExtensions`, and `AllowedContentTypes` are defined once here and referenced by both the validator and `LocalFileStorageService`. This ensures the two never drift apart.

## Why this implementation is appropriate

- **Dependency Inversion** — `IPdfExportService` and `IFileStorageService` are interfaces owned by the Application layer. Infrastructure provides the implementations, not the other way around. Controllers never import EF Core or QuestPDF.
- **FluentValidation** — rules are first-class objects, independently testable, and composable. They are registered once (`AddValidatorsFromAssembly`) and reused by both the API and the Web projects.
- **Single DTO / single validator** — `CreatePersonInput` + `CreatePersonInputValidator` serve both the REST API and the MVC web app, eliminating duplicated validation logic.
- **Photo constraints centralised** — `PhotoUploadConstraints` makes it impossible for the validator and the storage service to enforce different limits.

