using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PeopleManagement.Infrastructure.Persistence;

/// <summary>
/// Ensures the SQL Server schema matches EF Core migrations on application startup.
/// Keeps migration orchestration in Infrastructure (same layer as <see cref="PeopleManagementDbContext"/>).
/// </summary>
public static class HostExtensions
{
    public static IHost ApplyPeopleManagementMigrations(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeopleManagementDbContext>();
        db.Database.Migrate();
        return host;
    }
}
