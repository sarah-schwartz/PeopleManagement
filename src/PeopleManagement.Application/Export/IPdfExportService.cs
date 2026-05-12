using PeopleManagement.Domain.People;

namespace PeopleManagement.Application.Export;

/// <summary>Renders PDF exports without tying the Application layer to a specific PDF library.</summary>
public interface IPdfExportService
{
    Task<byte[]> ExportPeopleListAsync(IReadOnlyList<Person> people, CancellationToken cancellationToken = default);

    Task<byte[]> ExportPersonDetailsAsync(Person person, CancellationToken cancellationToken = default);
}
