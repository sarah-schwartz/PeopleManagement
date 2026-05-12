using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PeopleManagement.Application.Export;
using PeopleManagement.Application.Files;
using PeopleManagement.Application.People;
using PeopleManagement.Infrastructure.Export;
using PeopleManagement.Infrastructure.Files;
using PeopleManagement.Infrastructure.People;
using PeopleManagement.Infrastructure.Persistence;
using QuestPDF.Infrastructure;

namespace PeopleManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing. Configure it in appsettings.json (or user secrets) for SQL Server persistence.");
        }

        services.AddDbContext<PeopleManagementDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
            }));

        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IPdfExportService, QuestPdfPeopleExportService>();
        services.AddScoped<IPeopleService, PeopleService>();

        return services;
    }
}
