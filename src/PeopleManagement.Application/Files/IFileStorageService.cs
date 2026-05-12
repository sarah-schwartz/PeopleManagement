namespace PeopleManagement.Application.Files;

/// <summary>Stores binary assets such as profile photos; implementations live in Infrastructure.</summary>
public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string originalFileName, string contentType, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string storedPath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storedPath, CancellationToken cancellationToken = default);
}
