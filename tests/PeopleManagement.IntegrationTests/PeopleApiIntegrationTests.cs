using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PeopleManagement.IntegrationTests;

public sealed class PeopleApiIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PeopleApiIntegrationTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // Helper: build a multipart/form-data body for POST /api/People.
    private static MultipartFormDataContent BuildPersonForm(string firstName, string lastName, string email, string? phone = null)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(firstName), "firstName" },
            { new StringContent(lastName), "lastName" },
            { new StringContent(email), "email" }
        };
        if (phone is not null)
            form.Add(new StringContent(phone), "phone");
        return form;
    }

    [Fact]
    public async Task Create_person_then_list_contains_them()
    {
        var email = $"integration-{Guid.NewGuid():N}@example.com";

        var createResponse = await _client.PostAsync(
            "/api/People",
            BuildPersonForm("Integration", "User", email, "050-0000000"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(created.TryGetProperty("id", out var idProp));
        var newId = idProp.GetInt32();
        Assert.True(newId > 0);

        var listResponse = await _client.GetAsync("/api/People");
        listResponse.EnsureSuccessStatusCode();
        var people = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, people.ValueKind);

        var found = people.EnumerateArray().Any(p =>
            p.TryGetProperty("email", out var e) && e.GetString() == email);
        Assert.True(found, "Expected created person in list response.");
    }

    [Fact]
    public async Task Search_returns_matching_people_only()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        await _client.PostAsync("/api/People",
            BuildPersonForm("SearchUniqueAlpha", suffix, $"search-{suffix}@example.com"));
        await _client.PostAsync("/api/People",
            BuildPersonForm("Other", "Person Beta", $"other-{suffix}@example.com"));

        var searchResponse = await _client.GetAsync(
            $"/api/People/search?query={Uri.EscapeDataString("SearchUniqueAlpha")}");
        searchResponse.EnsureSuccessStatusCode();
        var results = await searchResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, results.ValueKind);

        var names = results.EnumerateArray()
            .Select(p => p.GetProperty("fullName").GetString())
            .ToList();

        Assert.Contains($"SearchUniqueAlpha {suffix}", names);
        Assert.DoesNotContain("Other Person Beta", names);
    }

    [Fact]
    public async Task Create_with_invalid_email_returns_bad_request()
    {
        var response = await _client.PostAsync(
            "/api/People",
            BuildPersonForm("Bad", "Email User", "not-an-email"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_duplicate_email_returns_internal_server_error()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.com";

        var first = await _client.PostAsync("/api/People", BuildPersonForm("First", "", email));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsync("/api/People", BuildPersonForm("Second", "", email));
        Assert.Equal(HttpStatusCode.InternalServerError, second.StatusCode);
    }
}
