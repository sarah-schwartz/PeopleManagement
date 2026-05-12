using System.Globalization;
using Microsoft.AspNetCore.Localization;
using PeopleManagement.Application;
using PeopleManagement.Infrastructure;
using PeopleManagement.Infrastructure.Middleware;
using PeopleManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
var supportedCultures = new[] { new CultureInfo("he-IL") };

builder.Services.AddControllersWithViews();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("he-IL");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.ApplyCurrentCultureToResponseHeaders = true;
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.ApplyPeopleManagementMigrations();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseGlobalExceptionHandling();
app.UseHttpsRedirection();
app.UseRequestLocalization();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=People}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
