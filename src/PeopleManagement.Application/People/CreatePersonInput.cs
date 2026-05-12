using PeopleManagement.Domain.People;

namespace PeopleManagement.Application.People;

/// <summary>Input for creating a person (HTTP/API); not mapped to a database table.</summary>
public sealed class CreatePersonInput
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public PersonStatus Status { get; init; } = PersonStatus.Active;

    public string? Phone { get; init; }

    public byte[]? PhotoContent { get; init; }

    public string? PhotoFileName { get; init; }

    public string? PhotoContentType { get; init; }
}
