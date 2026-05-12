using PeopleManagement.Domain.People;

namespace PeopleManagement.Application.People;

public interface IPeopleService
{
    Task<int> CreateAsync(CreatePersonInput input, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Person>> ListAsync(PersonStatus? status = null, CancellationToken cancellationToken = default);

    Task<Person?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Person>> SearchAsync(string query, CancellationToken cancellationToken = default);

    Task<byte[]> ExportPeopleListAsync(CancellationToken cancellationToken = default);

    Task<byte[]> ExportPeopleListByIdsAsync(IReadOnlyList<int> personIds, CancellationToken cancellationToken = default);
}
