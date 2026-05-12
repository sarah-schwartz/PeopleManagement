using Microsoft.AspNetCore.Hosting;
using PeopleManagement.Application.Files;
using PeopleManagement.Application.People.Validation;

namespace PeopleManagement.Infrastructure.Files;

/// <summary>
/// Stores files on the local file system under wwwroot/uploads/people.
/// Validates extension, content type, and file size using <see cref="PhotoUploadConstraints"/>.
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly string _uploadDirectory;

    public LocalFileStorageService(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));

        _uploadDirectory = Path.Combine(
            _webHostEnvironment.WebRootPath ?? throw new InvalidOperationException("WebRootPath is not configured"),
            "uploads",
            "people");

        EnsureUploadDirectoryExists();
    }

    public async Task<string> SaveAsync(Stream content, string originalFileName, string contentType, CancellationToken cancellationToken = default)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("Original file name cannot be empty", nameof(originalFileName));

        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!PhotoUploadConstraints.AllowedExtensions.Contains(extension))
            throw new InvalidOperationException($"File extension '{extension}' is not allowed. Allowed: {string.Join(", ", PhotoUploadConstraints.AllowedExtensions)}");

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type cannot be empty.", nameof(contentType));

        if (!PhotoUploadConstraints.AllowedContentTypes.Contains(contentType.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Content type '{contentType}' is not allowed.");
        }

        if (content.Length > PhotoUploadConstraints.MaxBytes)
            throw new InvalidOperationException($"File size exceeds maximum allowed size of {PhotoUploadConstraints.MaxBytes / (1024 * 1024)} MB");

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_uploadDirectory, fileName);

        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        await content.CopyToAsync(fileStream, cancellationToken);

        var relativePath = Path.Combine("uploads", "people", fileName).Replace("\\", "/");
        return relativePath;
    }

    public Task<Stream?> OpenReadAsync(string storedPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            throw new ArgumentException("Stored path cannot be empty", nameof(storedPath));

        var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, storedPath.Replace("/", Path.DirectorySeparatorChar.ToString()));

        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storedPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            throw new ArgumentException("Stored path cannot be empty", nameof(storedPath));

        var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, storedPath.Replace("/", Path.DirectorySeparatorChar.ToString()));

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    private void EnsureUploadDirectoryExists()
    {
        if (!Directory.Exists(_uploadDirectory))
            Directory.CreateDirectory(_uploadDirectory);
    }
}
