using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Export;
using PeopleManagement.Application.Files;
using PeopleManagement.Application.People;
using PeopleManagement.Domain.People;
using PeopleManagement.Infrastructure.Persistence;

namespace PeopleManagement.Infrastructure.People;

/// <summary>
/// Orchestrates people domain logic: creation, retrieval, searching, and PDF exports.
/// Uses EF Core <see cref="PeopleManagementDbContext"/> as the data access layer.
/// </summary>
public sealed class PeopleService : IPeopleService
{
    private readonly PeopleManagementDbContext _dbContext;
    private readonly IValidator<CreatePersonInput> _createPersonValidator;
    private readonly IPdfExportService _pdfExportService;
    private readonly IFileStorageService _fileStorageService;

    public PeopleService(
        PeopleManagementDbContext dbContext,
        IValidator<CreatePersonInput> createPersonValidator,
        IPdfExportService pdfExportService,
        IFileStorageService fileStorageService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _createPersonValidator = createPersonValidator ?? throw new ArgumentNullException(nameof(createPersonValidator));
        _pdfExportService = pdfExportService ?? throw new ArgumentNullException(nameof(pdfExportService));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
    }

    public async Task<int> CreateAsync(CreatePersonInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var validationResult = await _createPersonValidator.ValidateAsync(input, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var person = new Person(input.FirstName.Trim(), input.LastName.Trim(), input.Email.Trim(), input.Phone?.Trim(), input.Status);

        if (input.PhotoContent is { Length: > 0 }
            && !string.IsNullOrWhiteSpace(input.PhotoFileName)
            && !string.IsNullOrWhiteSpace(input.PhotoContentType))
        {
            await using var photoStream = new MemoryStream(input.PhotoContent, writable: false);
            var storedPath = await _fileStorageService.SaveAsync(
                photoStream,
                input.PhotoFileName,
                input.PhotoContentType,
                cancellationToken);
            person.SetProfilePhotoStoredPath(storedPath);
        }

        await _dbContext.People.AddAsync(person, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return person.Id;
    }

    public async Task<IReadOnlyList<Person>> ListAsync(PersonStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.People.AsNoTracking();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        var people = await query
            .OrderBy(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return people;
    }

    public async Task<Person?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var trimmed = (query ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(trimmed))
            return await ListAsync(null, cancellationToken);

        var lower = trimmed.ToLower();

        var dbQuery = _dbContext.People.AsNoTracking();

        if (lower.Contains(' '))
        {
            // Multi-word query (e.g. "moshe c"): also check the combined "FirstName LastName"
            // because neither field alone would match a term that spans the boundary.
            dbQuery = dbQuery.Where(p =>
                p.FirstName.ToLower().Contains(lower) ||
                p.LastName.ToLower().Contains(lower) ||
                (p.FirstName + " " + p.LastName).ToLower().Contains(lower));
        }
        else
        {
            // Single-word query: checking each field is sufficient —
            // the concatenated form cannot match anything the individual fields don't already cover.
            dbQuery = dbQuery.Where(p =>
                p.FirstName.ToLower().Contains(lower) ||
                p.LastName.ToLower().Contains(lower));
        }

        return await dbQuery
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<byte[]> ExportPeopleListAsync(CancellationToken cancellationToken = default)
    {
        var people = await ListAsync(null, cancellationToken);
        return await _pdfExportService.ExportPeopleListAsync(people, cancellationToken);
    }

    public async Task<byte[]> ExportPeopleListByIdsAsync(IReadOnlyList<int> personIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(personIds);

        var distinctOrdered = new List<int>();
        var seen = new HashSet<int>();
        foreach (var id in personIds)
        {
            if (id <= 0 || !seen.Add(id))
                continue;
            distinctOrdered.Add(id);
        }

        if (distinctOrdered.Count == 0)
            return await _pdfExportService.ExportPeopleListAsync(Array.Empty<Person>(), cancellationToken);

        var idSet = distinctOrdered.ToHashSet();
        var entities = await _dbContext.People
            .AsNoTracking()
            .Where(p => idSet.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var byId = entities.ToDictionary(p => p.Id);
        var ordered = new List<Person>(distinctOrdered.Count);
        foreach (var id in distinctOrdered)
        {
            if (byId.TryGetValue(id, out var entity))
                ordered.Add(entity);
        }

        return await _pdfExportService.ExportPeopleListAsync(ordered, cancellationToken);
    }
}
