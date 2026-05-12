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

        var person = new Person(input.FullName.Trim(), input.Email.Trim(), input.Phone?.Trim());

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

    public async Task<IReadOnlyList<Person>> ListAsync(CancellationToken cancellationToken = default)
    {
        var people = await _dbContext.People
            .AsNoTracking()
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
        var trimmedQuery = (query ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(trimmedQuery))
            return await ListAsync(cancellationToken);

        var normalizedQuery = trimmedQuery.ToLower();

        var people = await _dbContext.People
            .AsNoTracking()
            .Where(p => p.FullName.ToLower().Contains(normalizedQuery))
            .OrderBy(p => p.FullName)
            .ToListAsync(cancellationToken);

        return people;
    }

    public async Task<byte[]> ExportPeopleListAsync(CancellationToken cancellationToken = default)
    {
        var people = await ListAsync(cancellationToken);
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
