using FluentValidation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using PeopleManagement.Application.Export;
using PeopleManagement.Application.Files;
using PeopleManagement.Application.People;
using PeopleManagement.Application.People.Validators;
using PeopleManagement.Domain.People;
using PeopleManagement.Infrastructure.People;
using PeopleManagement.Infrastructure.Persistence;

namespace PeopleManagement.UnitTests;

public sealed class PeopleServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PeopleManagementDbContext _dbContext;
    private readonly Mock<IPdfExportService> _pdfExport = new();
    private readonly Mock<IFileStorageService> _fileStorage = new();
    private readonly PeopleService _sut;

    public PeopleServiceTests()
    {
        // SQLite in-memory: keep the connection open so the database persists for the test lifetime.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PeopleManagementDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new PeopleManagementDbContext(options);
        _dbContext.Database.EnsureCreated();

        _pdfExport
            .Setup(p => p.ExportPeopleListAsync(It.IsAny<IReadOnlyList<Person>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<byte>());

        _fileStorage
            .Setup(f => f.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("uploads/people/test.png");

        _sut = new PeopleService(
            _dbContext,
            new CreatePersonInputValidator(),
            _pdfExport.Object,
            _fileStorage.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ListAsync_returns_empty_when_database_is_empty()
    {
        var list = await _sut.ListAsync();
        Assert.Empty(list);
    }

    [Fact]
    public async Task CreateAsync_persists_person_and_returns_id()
    {
        var input = new CreatePersonInput
        {
            FirstName = "  Unit Test  ",
            LastName = "  User  ",
            Email = "unit-test@example.com",
            Phone = "050-1234567"
        };

        var id = await _sut.CreateAsync(input);

        Assert.True(id > 0);
        var stored = await _dbContext.People.AsNoTracking().SingleAsync();
        Assert.Equal("Unit Test", stored.FirstName);
        Assert.Equal("User", stored.LastName);
        Assert.Equal("Unit Test User", stored.FullName);
        Assert.Equal("unit-test@example.com", stored.Email);
        Assert.Equal("050-1234567", stored.Phone);
        Assert.Equal(PersonStatus.Active, stored.Status);
    }

    [Fact]
    public async Task CreateAsync_persists_inactive_status_when_specified()
    {
        var id = await _sut.CreateAsync(new CreatePersonInput
        {
            FirstName = "Inactive",
            LastName = "Person",
            Email = "inactive@example.com",
            Status = PersonStatus.Inactive
        });

        var stored = await _dbContext.People.AsNoTracking().SingleAsync(p => p.Id == id);
        Assert.Equal(PersonStatus.Inactive, stored.Status);
    }

    [Fact]
    public async Task ListAsync_filters_by_status()
    {
        await _sut.CreateAsync(new CreatePersonInput { FirstName = "Active", LastName = "One", Email = "a@e.com", Status = PersonStatus.Active });
        await _sut.CreateAsync(new CreatePersonInput { FirstName = "Inactive", LastName = "Two", Email = "b@e.com", Status = PersonStatus.Inactive });

        var activeOnly = await _sut.ListAsync(PersonStatus.Active);
        var inactiveOnly = await _sut.ListAsync(PersonStatus.Inactive);
        var all = await _sut.ListAsync();

        Assert.Single(activeOnly);
        Assert.Equal(PersonStatus.Active, activeOnly[0].Status);

        Assert.Single(inactiveOnly);
        Assert.Equal(PersonStatus.Inactive, inactiveOnly[0].Status);

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task CreateAsync_with_photo_calls_file_storage()
    {
        var input = new CreatePersonInput
        {
            FirstName = "Photo",
            LastName = "User",
            Email = "photo-user@example.com",
            Phone = null,
            PhotoContent = new byte[] { 1, 2, 3 },
            PhotoFileName = "avatar.png",
            PhotoContentType = "image/png"
        };

        await _sut.CreateAsync(input);

        _fileStorage.Verify(
            f => f.SaveAsync(It.IsAny<Stream>(), "avatar.png", "image/png", It.IsAny<CancellationToken>()),
            Times.Once);

        var person = await _dbContext.People.AsNoTracking().SingleAsync();
        Assert.Equal("uploads/people/test.png", person.ProfilePhotoStoredPath);
    }

    [Fact]
    public async Task CreateAsync_invalid_email_throws_ValidationException()
    {
        var input = new CreatePersonInput
        {
            FirstName = "Bad",
            LastName = "",
            Email = "not-valid",
            Phone = null
        };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(input));
    }

    [Fact]
    public async Task CreateAsync_duplicate_email_throws_DbUpdateException()
    {
        var input = new CreatePersonInput { FirstName = "First", LastName = "", Email = "dup@example.com", Phone = "" };
        await _sut.CreateAsync(input);

        var duplicate = new CreatePersonInput { FirstName = "Second", LastName = "", Email = "dup@example.com", Phone = "" };
        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() => _sut.CreateAsync(duplicate));
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_missing()
    {
        var result = await _sut.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task SearchAsync_empty_query_behaves_like_list_all()
    {
        await _sut.CreateAsync(new CreatePersonInput
        {
            FirstName = "Alice",
            LastName = "",
            Email = "alice@example.com",
            Phone = ""
        });
        await _sut.CreateAsync(new CreatePersonInput
        {
            FirstName = "Bob",
            LastName = "",
            Email = "bob@example.com",
            Phone = ""
        });

        var bySearch = await _sut.SearchAsync("   ");
        var byList = await _sut.ListAsync();

        Assert.Equal(byList.Count, bySearch.Count);
    }

    [Fact]
    public async Task SearchAsync_filters_by_name_case_insensitive()
    {
        await _sut.CreateAsync(new CreatePersonInput
        {
            FirstName = "Moshe",
            LastName = "Cohen",
            Email = "moshe@example.com",
            Phone = ""
        });
        await _sut.CreateAsync(new CreatePersonInput
        {
            FirstName = "Bracha",
            LastName = "Levi",
            Email = "bracha@example.com",
            Phone = ""
        });

        var results = await _sut.SearchAsync("COHEN");

        Assert.Single(results);
        Assert.Equal("Moshe Cohen", results[0].FullName);
    }

    [Fact]
    public async Task ExportPeopleListAsync_delegates_to_pdf_service()
    {
        await _sut.CreateAsync(new CreatePersonInput
        {
            FirstName = "Pdf",
            LastName = "Row",
            Email = "pdf@example.com",
            Phone = ""
        });

        await _sut.ExportPeopleListAsync();

        _pdfExport.Verify(
            p => p.ExportPeopleListAsync(
                It.Is<IReadOnlyList<Person>>(list => list.Count == 1 && list[0].FullName == "Pdf Row"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportPeopleListByIdsAsync_preserves_order_and_skips_unknown_ids()
    {
        var id1 = await _sut.CreateAsync(new CreatePersonInput { FirstName = "First", LastName = "", Email = "a1@e.com", Phone = "" });
        var id2 = await _sut.CreateAsync(new CreatePersonInput { FirstName = "Second", LastName = "", Email = "a2@e.com", Phone = "" });

        await _sut.ExportPeopleListByIdsAsync(new[] { id2, 99999, id1 });

        _pdfExport.Verify(
            p => p.ExportPeopleListAsync(
                It.Is<IReadOnlyList<Person>>(list =>
                    list.Count == 2
                    && list[0].FullName == "Second"
                    && list[1].FullName == "First"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
