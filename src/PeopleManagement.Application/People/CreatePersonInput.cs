namespace PeopleManagement.Application.People;

/// <summary>Input for creating a person (HTTP/API); not mapped to a database table.</summary>
public sealed class CreatePersonInput
{
    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public byte[]? PhotoContent { get; init; }

    public string? PhotoFileName { get; init; }

    public string? PhotoContentType { get; init; }
}
