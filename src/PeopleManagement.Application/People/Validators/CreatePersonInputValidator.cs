using FluentValidation;
using FluentValidation.Validators;
using PeopleManagement.Application.People.Validation;
using PeopleManagement.Domain.People;

namespace PeopleManagement.Application.People.Validators;

public sealed class CreatePersonInputValidator : AbstractValidator<CreatePersonInput>
{
    public CreatePersonInputValidator()
    {
        RuleFor(x => x.FirstName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("יש להזין שם פרטי.")
            .Must(static name => name.Trim().Length <= Person.FirstNameMaxLength)
            .WithMessage($"אורך השם הפרטי יכול להיות עד {Person.FirstNameMaxLength} תווים.");

        When(x => !string.IsNullOrWhiteSpace(x.LastName), () =>
        {
            RuleFor(x => x.LastName)
                .MaximumLength(Person.LastNameMaxLength)
                .WithMessage($"אורך שם המשפחה יכול להיות עד {Person.LastNameMaxLength} תווים.");
        });

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("יש להזין כתובת דוא\"ל.")
            .Must(static email => email.Trim().Length <= Person.EmailMaxLength)
            .WithMessage($"כתובת הדוא\"ל יכולה להכיל עד {Person.EmailMaxLength} תווים.")
            .EmailAddress(EmailValidationMode.AspNetCoreCompatible)
            .WithMessage("כתובת הדוא\"ל אינה תקינה.");

        When(x => !string.IsNullOrWhiteSpace(x.Phone), () =>
        {
            RuleFor(x => x.Phone)
                .MaximumLength(Person.PhoneMaxLength)
                .WithMessage($"מספר הטלפון יכול להכיל עד {Person.PhoneMaxLength} תווים.");
        });

        When(PhotoFieldsProvided, () =>
        {
            RuleFor(x => x.PhotoContent)
                .NotNull()
                .Must(static bytes => bytes!.Length > 0)
                .WithMessage("יש לצרף תוכן קובץ בעת העלאת תמונה.")
                .Must(static bytes => bytes!.LongLength <= PhotoUploadConstraints.MaxBytes)
                .WithMessage($"גודל התמונה יכול להיות עד {PhotoUploadConstraints.MaxBytes / (1024 * 1024)}MB.");

            RuleFor(x => x.PhotoFileName)
                .NotEmpty()
                .MaximumLength(260)
                .Must(HaveAllowedPhotoExtension)
                .WithMessage($"ניתן להעלות תמונה רק מהסיומות הבאות: {string.Join(", ", PhotoUploadConstraints.AllowedExtensions)}.");

            RuleFor(x => x.PhotoContentType)
                .NotEmpty()
                .MaximumLength(128)
                .Must(BeAllowedContentType)
                .WithMessage("סוג קובץ התמונה אינו נתמך.");
        });

        When(
            dto => HasPartialPhotoAttempt(dto),
            () =>
            {
                RuleFor(x => x)
                    .Must(static dto => PhotoFieldsProvided(dto))
                    .WithMessage("יש לשלוח יחד את שם הקובץ, סוג התוכן ותוכן התמונה.");
            });
    }

    private static bool PhotoFieldsProvided(CreatePersonInput dto) =>
        dto.PhotoContent is { Length: > 0 }
        && !string.IsNullOrWhiteSpace(dto.PhotoFileName)
        && !string.IsNullOrWhiteSpace(dto.PhotoContentType);

    private static bool HasPartialPhotoAttempt(CreatePersonInput dto)
    {
        var hasContent = dto.PhotoContent is { Length: > 0 };
        var hasName = !string.IsNullOrWhiteSpace(dto.PhotoFileName);
        var hasType = !string.IsNullOrWhiteSpace(dto.PhotoContentType);
        var count = (hasContent ? 1 : 0) + (hasName ? 1 : 0) + (hasType ? 1 : 0);
        return count is > 0 and < 3;
    }

    private static bool HaveAllowedPhotoExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return PhotoUploadConstraints.AllowedExtensions.Contains(extension);
    }

    private static bool BeAllowedContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        return PhotoUploadConstraints.AllowedContentTypes.Contains(contentType.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
