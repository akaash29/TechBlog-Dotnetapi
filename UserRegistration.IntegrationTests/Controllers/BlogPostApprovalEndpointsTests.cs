using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.IntegrationTests.Infrastructure;

namespace UserRegistration.IntegrationTests.Controllers;

/// <summary>Covers the editorial workflow added for BlogPostStatus: a
/// Writer's publish lands in PendingApproval, and only an Admin can
/// approve (→ Published) or reject (→ post + its images gone).</summary>
public sealed class BlogPostApprovalEndpointsTests : IntegrationTestBase
{
    private const int SeededCategoryId = 1;

    public BlogPostApprovalEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_AsWriter_Publishing_LandsInPendingApproval()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client, role: "Writer");

        var response = await Client.PostAsJsonAsync("/api/blogposts", new
        {
            Title = "A writer's post",
            Description = "Standfirst.",
            PostHtml = "<p>Body</p>",
            CoverImagePath = (string?)null,
            CategoryId = SeededCategoryId,
            IsDraft = false
        });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PendingApproval", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Create_AsAdmin_Publishing_IsPublishedImmediately()
    {
        await TestAuth.SeedAndAuthenticateAdminAsync(Factory.Services, Client);

        var response = await Client.PostAsJsonAsync("/api/blogposts", new
        {
            Title = "An admin's post",
            Description = "Standfirst.",
            PostHtml = "<p>Body</p>",
            CoverImagePath = (string?)null,
            CategoryId = SeededCategoryId,
            IsDraft = false
        });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Published", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetPending_AsAdmin_ReturnsOnlyPendingPosts()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client, role: "Writer");
        var pendingId = await BlogPostTestHelper.CreatePostAsync(Client, title: "Awaiting review");
        var draftId = await BlogPostTestHelper.CreatePostAsync(Client, isDraft: true, title: "Still a draft");

        using var admin = Factory.CreateClient();
        await TestAuth.SeedAndAuthenticateAdminAsync(Factory.Services, admin);

        var response = await admin.GetAsync("/api/blogposts/pending");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        var ids = items!.Select(i => i.GetProperty("id").GetInt32()).ToList();

        Assert.Contains(pendingId, ids);
        Assert.DoesNotContain(draftId, ids);
    }

    [Fact]
    public async Task GetPending_AsNonAdmin_ReturnsForbidden()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client, role: "Writer");

        var response = await Client.GetAsync("/api/blogposts/pending");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Approve_AsAdmin_PublishesThePost()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client, role: "Writer");
        var id = await BlogPostTestHelper.CreatePostAsync(Client, title: "Please approve me");

        using var admin = Factory.CreateClient();
        await TestAuth.SeedAndAuthenticateAdminAsync(Factory.Services, admin);

        var response = await admin.PostAsync($"/api/blogposts/{id}/approve", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Published", body.GetProperty("status").GetString());

        // Now visible in the feed, same as any other published post.
        var feedResponse = await admin.GetAsync("/api/blogposts/feed?tab=latest&page=1&pageSize=50");
        var feedBody = await feedResponse.Content.ReadFromJsonAsync<JsonElement>();
        var feedIds = feedBody.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32());
        Assert.Contains(id, feedIds);
    }

    [Fact]
    public async Task Approve_AsNonAdmin_ReturnsForbidden()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client, role: "Writer");
        var id = await BlogPostTestHelper.CreatePostAsync(Client);

        var response = await Client.PostAsync($"/api/blogposts/{id}/approve", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Approve_AnAlreadyPublishedPost_ReturnsConflict()
    {
        await TestAuth.SeedAndAuthenticateAdminAsync(Factory.Services, Client);
        var id = await BlogPostTestHelper.CreatePostAsync(Client);

        var response = await Client.PostAsync($"/api/blogposts/{id}/approve", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Reject_AsAdmin_DeletesThePostAndItsBlobImages()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client, role: "Writer");

        using var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[1024]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(fileContent, "file", "cover.jpg");
        var uploadResponse = await Client.PostAsync("/api/images/upload", uploadContent);
        var uploadBody = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
        var coverImagePath = uploadBody.GetProperty("imagePath").GetString();

        var createResponse = await Client.PostAsJsonAsync("/api/blogposts", new
        {
            Title = "Please reject me",
            Description = "Standfirst.",
            PostHtml = "<p>Body</p>",
            CoverImagePath = coverImagePath,
            CategoryId = SeededCategoryId,
            IsDraft = false
        });
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = createBody.GetProperty("id").GetInt32();

        using var admin = Factory.CreateClient();
        await TestAuth.SeedAndAuthenticateAdminAsync(Factory.Services, admin);

        var rejectResponse = await admin.PostAsync($"/api/blogposts/{id}/reject", content: null);
        Assert.Equal(HttpStatusCode.NoContent, rejectResponse.StatusCode);

        var getResponse = await admin.GetAsync($"/api/blogposts/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        var fakeBlobStorage = (FakeBlobStorageService)Factory.Services.GetRequiredService<IBlobStorageService>();
        Assert.Contains(coverImagePath, fakeBlobStorage.DeletedUrls);
    }

    [Fact]
    public async Task Reject_AsNonAdmin_ReturnsForbidden()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client, role: "Writer");
        var id = await BlogPostTestHelper.CreatePostAsync(Client);

        var response = await Client.PostAsync($"/api/blogposts/{id}/reject", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMyStats_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/blogposts/my-stats");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyStats_ReflectsOnlyThisAuthorsOwnPublishedPosts()
    {
        await TestAuth.SeedAndAuthenticateAdminAsync(Factory.Services, Client);
        await BlogPostTestHelper.CreatePostAsync(Client, title: "Mine, published");
        await BlogPostTestHelper.CreatePostAsync(Client, isDraft: true, title: "Mine, still a draft");

        using var other = Factory.CreateClient();
        await TestAuth.SeedAndAuthenticateAdminAsync(Factory.Services, other);
        await BlogPostTestHelper.CreatePostAsync(other, title: "Someone else's, published");

        var response = await Client.GetAsync("/api/blogposts/my-stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("posts").GetInt32());
        Assert.Equal(7, body.GetProperty("readsThisWeek").EnumerateArray().Count());
    }
}
