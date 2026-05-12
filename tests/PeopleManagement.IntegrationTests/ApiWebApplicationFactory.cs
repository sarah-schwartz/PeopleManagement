using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PeopleManagement.Api;
using PeopleManagement.Infrastructure.Persistence;

namespace PeopleManagement.IntegrationTests;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    // A single open connection keeps the in-memory SQLite database alive for the
    // lifetime of the factory (one per IClassFixture instance = one per test class).
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public ApiWebApplicationFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SkipEfMigrations"] = "true"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove the SQL Server DbContext registrations added by AddInfrastructure().
            var toRemove = services
                .Where(d =>
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>) &&
                     d.ServiceType.GetGenericArguments()[0] == typeof(PeopleManagementDbContext)) ||
                    d.ServiceType == typeof(DbContext) ||
                    d.ServiceType == typeof(PeopleManagementDbContext) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>) &&
                     d.ServiceType.GetGenericArguments()[0] == typeof(PeopleManagementDbContext)))
                .ToList();

            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            // Register a SQLite DbContext that shares the persistent in-memory connection.
            // This means unique indexes (e.g. UX_People_Email) are enforced, unlike InMemory.
            services.AddDbContext<PeopleManagementDbContext>(options =>
                options.UseSqlite(_connection));

            // Replace file storage with a no-op stub so no files are written to disk.
            var fileStorageDescriptors = services
                .Where(d => d.ServiceType == typeof(PeopleManagement.Application.Files.IFileStorageService))
                .ToList();

            foreach (var descriptor in fileStorageDescriptors)
                services.Remove(descriptor);

            services.AddScoped<PeopleManagement.Application.Files.IFileStorageService, TestFileStorageService>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Create the SQLite schema once after the host (and therefore the DbContext) is configured.
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeopleManagementDbContext>();
        db.Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _connection.Dispose();
        base.Dispose(disposing);
    }
}

/// <summary>
/// No-op file storage: tests do not write files to disk.
/// </summary>
public sealed class TestFileStorageService : PeopleManagement.Application.Files.IFileStorageService
{
    public Task<string> SaveAsync(Stream content, string originalFileName, string contentType, CancellationToken cancellationToken = default)
        => Task.FromResult($"uploads/people/{Guid.NewGuid()}{Path.GetExtension(originalFileName)}");

    public Task<Stream?> OpenReadAsync(string storedPath, CancellationToken cancellationToken = default)
        => Task.FromResult<Stream?>(null);

    public Task DeleteAsync(string storedPath, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
