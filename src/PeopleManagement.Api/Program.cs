using PeopleManagement.Application;
using PeopleManagement.Infrastructure;
using PeopleManagement.Infrastructure.Middleware;
using PeopleManagement.Infrastructure.Persistence;

namespace PeopleManagement.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        var app = builder.Build();

        var skipMigrations = string.Equals(app.Configuration["SkipEfMigrations"], "true", StringComparison.OrdinalIgnoreCase);
        if (!skipMigrations)
            app.ApplyPeopleManagementMigrations();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseGlobalExceptionHandling();
        app.UseStaticFiles();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
