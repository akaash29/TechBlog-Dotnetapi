using System.Net;
using System.Net.Http.Json;
using UserRegistration.IntegrationTests.Infrastructure;

namespace UserRegistration.IntegrationTests.Controllers;

public sealed class BlogPostsEndpointsTests : IntegrationTestBase
{
    private const int SeededCategoryId = 1; // "Technology News" — see CategoryConfiguration

    public BlogPostsEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_Published_WithAllRequiredFields_ReturnsCreated()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        var response = await Client.PostAsJsonAsync("/api/blogposts", new
        {
            Title = "A published post",
            Description = "A short standfirst.",
            PostHtml = "<p>Body</p>",
            CoverImagePath = (string?)null,
            CategoryId = SeededCategoryId,
            IsDraft = false
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_Published_WithoutTitle_ReturnsBadRequest()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        var response = await Client.PostAsJsonAsync("/api/blogposts", new
        {
            Title = "",
            Description = "A short standfirst.",
            PostHtml = "<p>Body</p>",
            CoverImagePath = (string?)null,
            CategoryId = SeededCategoryId,
            IsDraft = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Draft_WithOnlyCategory_ReturnsCreated()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        // Drafts can be saved incomplete — only Publish requires the full set.
        var response = await Client.PostAsJsonAsync("/api/blogposts", new
        {
            Title = "",
            Description = "",
            PostHtml = "",
            CoverImagePath = (string?)null,
            CategoryId = SeededCategoryId,
            IsDraft = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutCategory_ReturnsBadRequest()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        var response = await Client.PostAsJsonAsync("/api/blogposts", new
        {
            Title = "",
            Description = "",
            PostHtml = "",
            CoverImagePath = (string?)null,
            CategoryId = 0,
            IsDraft = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithUnknownCategory_ReturnsNotFound()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        var response = await Client.PostAsJsonAsync("/api/blogposts", new
        {
            Title = "A post",
            Description = "A standfirst.",
            PostHtml = "<p>Body</p>",
            CoverImagePath = (string?)null,
            CategoryId = 999_999,
            IsDraft = false
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/blogposts", new
        {
            Title = "A post",
            Description = "A standfirst.",
            PostHtml = "<p>Body</p>",
            CoverImagePath = (string?)null,
            CategoryId = SeededCategoryId,
            IsDraft = false
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
