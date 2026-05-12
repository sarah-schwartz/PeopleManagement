namespace PeopleManagement.IntegrationTests;

public sealed class ApiSmokeTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiSmokeTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Swagger_document_returns_success()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task People_list_endpoint_returns_success()
    {
        var response = await _client.GetAsync("/api/People");
        response.EnsureSuccessStatusCode();
    }
}
