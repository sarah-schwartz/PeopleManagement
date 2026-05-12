using Microsoft.Extensions.DependencyInjection;
using PeopleManagement.Application;

namespace PeopleManagement.UnitTests;

public sealed class ApplicationDependencyInjectionTests
{
    [Fact]
    public void AddApplication_registers_without_throwing()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        Assert.NotNull(services);
    }
}
