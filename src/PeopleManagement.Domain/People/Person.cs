namespace PeopleManagement.Domain.People;

/// <summary>Represents a person stored in the database.</summary>
public sealed class Person
{
    public const int FirstNameMaxLength = 100;

    public const int LastNameMaxLength = 100;

    /// <summary>
    /// Maximum persisted email length (RFC-friendly practical cap).
    /// </summary>
    public const int EmailMaxLength = 320;

    /// <summary>
    /// Maximum persisted phone length.
    /// </summary>
    public const int PhoneMaxLength = 20;

    /// <summary>
    /// Maximum length for <see cref="ProfilePhotoStoredPath"/> (relative path under web root, e.g. uploads/people/...).
    /// </summary>
    public const int ProfilePhotoStoredPathMaxLength = 512;

    private Person()
    {
    }

    public Person(string firstName, string lastName, string email, string? phone = null, PersonStatus status = PersonStatus.Active)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone ?? string.Empty;
        Status = status;
    }

    public int Id { get; private set; }

    public PersonStatus Status { get; private set; } = PersonStatus.Active;

    public string FirstName { get; private set; } = default!;

    public string LastName { get; private set; } = default!;

    /// <summary>Computed display name — not stored in the database.</summary>
    public string FullName => string.IsNullOrWhiteSpace(LastName)
        ? FirstName
        : $"{FirstName} {LastName}";

    public string Email { get; private set; } = default!;

    public string Phone { get; private set; } = string.Empty;

    /// <summary>Optional relative path returned by file storage (e.g. uploads/people/guid.jpg).</summary>
    public string? ProfilePhotoStoredPath { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    internal void StampAsCreated(DateTime utcNow)
    {
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    internal void StampAsModified(DateTime utcNow)
    {
        UpdatedAtUtc = utcNow;
    }

    internal void SetProfilePhotoStoredPath(string? relativeStoredPath)
    {
        ProfilePhotoStoredPath = relativeStoredPath;
    }

    internal void Activate() => Status = PersonStatus.Active;

    internal void Deactivate() => Status = PersonStatus.Inactive;
}
