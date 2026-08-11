using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UserRegistration.IntegrationTests.Infrastructure;

namespace UserRegistration.IntegrationTests.Controllers;

public sealed class CategoriesEndpointsTests : IntegrationTestBase
{
    public CategoriesEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsTheFiveSeededCategories()
    {
        var response = await Client.GetAsync("/api/categories");

        var categories = await response.Content.ReadFromJsonAsync<JsonElement>();
        var names = categories.EnumerateArray().Select(c => c.GetProperty("name").GetString()).ToList();

        Assert.Equal(5, names.Count);
        Assert.Contains("Technology News", names);
        Assert.Contains("Cybersecurity", names);
    }
}
