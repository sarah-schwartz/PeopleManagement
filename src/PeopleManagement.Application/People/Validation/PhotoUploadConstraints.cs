namespace PeopleManagement.Application.People.Validation;

/// <summary>Central limits for optional photo uploads (aligned with typical MVC/multipart constraints).</summary>
public static class PhotoUploadConstraints
{
    public const long MaxBytes = 5 * 1024 * 1024;

    public static readonly string[] AllowedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    ];

    public static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];
}
