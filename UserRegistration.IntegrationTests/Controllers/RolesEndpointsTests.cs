using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UserRegistration.IntegrationTests.Infrastructure;

namespace UserRegistration.IntegrationTests.Controllers;

public sealed class RolesEndpointsTests : IntegrationTestBase
{
    public RolesEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsOk()
    {
        // AllowAnonymous — the register form's role picker needs this before sign-in exists.
        var response = await Client.GetAsync("/api/roles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsTheFiveSeededRoles()
    {
        var response = await Client.GetAsync("/api/roles");

        var roles = await response.Content.ReadFromJsonAsync<JsonElement>();
        var names = roles.EnumerateArray().Select(r => r.GetProperty("name").GetString()).ToList();

        Assert.Equal(5, names.Count);
        Assert.Contains("Writer", names);
        Assert.Contains("Admin", names);
    }
}
