using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UserRegistration.IntegrationTests.Infrastructure;

namespace UserRegistration.IntegrationTests.Controllers;

/// <summary>Covers the feed/journal/top/detail/view/like endpoints added for
/// real-data feed & journal pages. The repository behind these picks a SQL
/// Server stored procedure or a LINQ fallback depending on the provider
/// (see BlogPostRepository) — this SQLite-backed test factory always
/// exercises the LINQ fallback, so it verifies the query *logic*, not the
/// stored procedures themselves (those only run against real SQL Server).</summary>
public sealed class BlogPostsFeedEndpointsTests : IntegrationTestBase
{
    private const int SeededCategoryId = 1;
    private const int OtherSeededCategoryId = 2;

    public BlogPostsFeedEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetFeed_Latest_ReturnsPublishedPostsNewestFirst()
    {
        // Admin so "publish" lands as Published immediately — a Writer's
        // publish would go to PendingApproval and never show up in the feed.
        await TestAuth.SeedAndAuthenticateAdminAsync(Factory.Services, Client);
        var firstId = await BlogPostTestHelper.CreatePostAsync(Client, title: "Older post");
        var secondId = await BlogPostTestHelper.CreatePostAsync(Client, title: "Newer post");

        var response = await Client.GetAsync("/api/blogposts/feed?tab=latest&page=1&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("items").EnumerateArray().ToList();
        var ids = items.Select(i => i.GetProperty("id").GetInt32()).ToList();

        Assert.Contains(secondId, ids);
        Assert.Contains(firstId, ids);
        Assert.True(ids.IndexOf(secondId) < ids.IndexOf(firstId));
    }

    [Fact]
    public async Task GetFeed_WithInvalidTab_ReturnsBadRequest()
    {
        var response = await Client.GetAsync("/api/blogposts/feed?tab=sideways");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetFeed_DoesNotIncludeDrafts()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);
        var draftId = await BlogPostTestHelper.CreatePostAsync(Client, isDraft: true, title: "A draft");

        var response = await Client.GetAsync("/api/blogposts/feed?tab=latest&page=1&pageSize=50");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var ids = body.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32());

        Assert.DoesNotContain(draftId, ids);
    }

    [Fact]
    public async Task GetJournal_FilteredByCategory_ReturnsOnlyThatCategory()
    {
        await TestAuth.SeedAndAuthenticateAdminAsync(Factory.Services, Client);
        var inCategoryId = await BlogPostTestHelper.CreatePostAsync(Client, categoryId: OtherSeededCategoryId, title: "In category");
        var outOfCategoryId = await BlogPostTestHelper.CreatePostAsync(Client, categoryId: SeededCategoryId, title: "Out of category");

        var response = await Client.GetAsync($"/api/blogposts/journal?categoryId={OtherSeededCategoryId}&page=1&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var ids = body.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32()).ToList();

        Assert.Contains(inCategoryId, ids);
        Assert.DoesNotContain(outOfCategoryId, ids);
    }

    [Fact]
    public async Task GetTop_ByViews_ReturnsDescendingOrder()
    {
        await TestAuth.SeedAndAuthenticateAdminAsync(Factory.Services, Client);
        var lowId = await BlogPostTestHelper.CreatePostAsync(Client, title: "Barely read");
        var highId = await BlogPostTestHelper.CreatePostAsync(Client, title: "Widely read");

        // three views for highId, none extra for lowId
        for (var i = 0; i < 3; i++)
        {
            await Client.PostAsync($"/api/blogposts/{highId}/view", content: null);
        }

        var response = await Client.GetAsync("/api/blogposts/top?metric=views&take=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        var ids = items!.Select(i => i.GetProperty("id").GetInt32()).ToList();

        Assert.True(ids.IndexOf(highId) < ids.IndexOf(lowId));
    }

    [Fact]
    public async Task GetById_ForPublishedPost_ReturnsOk()
    {
        await TestAuth.SeedAndAuthenticateAdminAsync(Factory.Services, Client);
        var id = await BlogPostTestHelper.CreatePostAsync(Client);

        var response = await Client.GetAsync($"/api/blogposts/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForUnknownPost_ReturnsNotFound()
    {
        var response = await Client.GetAsync("/api/blogposts/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForDraftAsOwner_ReturnsOk()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);
        var id = await BlogPostTestHelper.CreatePostAsync(Client, isDraft: true);

        var response = await Client.GetAsync($"/api/blogposts/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForDraftAsAnotherUser_ReturnsNotFound()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);
        var id = await BlogPostTestHelper.CreatePostAsync(Client, isDraft: true);

        using var other = Factory.CreateClient();
        await TestAuth.RegisterAndAuthenticateAsync(other);

        var response = await other.GetAsync($"/api/blogposts/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RecordView_IncrementsViewCount()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);
        var id = await BlogPostTestHelper.CreatePostAsync(Client);

        var viewResponse = await Client.PostAsync($"/api/blogposts/{id}/view", content: null);
        Assert.Equal(HttpStatusCode.NoContent, viewResponse.StatusCode);

        var detail = await Client.GetFromJsonAsync<JsonElement>($"/api/blogposts/{id}");
        Assert.Equal(1, detail.GetProperty("viewCount").GetInt32());
    }

    [Fact]
    public async Task Like_TogglesOnThenOff()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);
        var id = await BlogPostTestHelper.CreatePostAsync(Client);

        var onResponse = await Client.PostAsync($"/api/blogposts/{id}/like", content: null);
        Assert.Equal(HttpStatusCode.OK, onResponse.StatusCode);
        var onBody = await onResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(onBody.GetProperty("liked").GetBoolean());
        Assert.Equal(1, onBody.GetProperty("likesCount").GetInt32());

        var offResponse = await Client.PostAsync($"/api/blogposts/{id}/like", content: null);
        var offBody = await offResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(offBody.GetProperty("liked").GetBoolean());
        Assert.Equal(0, offBody.GetProperty("likesCount").GetInt32());
    }

    [Fact]
    public async Task Like_WithoutAuthentication_ReturnsUnauthorized()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);
        var id = await BlogPostTestHelper.CreatePostAsync(Client);

        using var anon = Factory.CreateClient();
        var response = await anon.PostAsync($"/api/blogposts/{id}/like", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLiked_ReflectsPriorLikes_AndIsEmptyForAnonymous()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);
        var likedId = await BlogPostTestHelper.CreatePostAsync(Client);
        var unlikedId = await BlogPostTestHelper.CreatePostAsync(Client);
        await Client.PostAsync($"/api/blogposts/{likedId}/like", content: null);

        var likedIds = await Client.GetFromJsonAsync<List<int>>("/api/blogposts/liked");
        Assert.Contains(likedId, likedIds!);
        Assert.DoesNotContain(unlikedId, likedIds!);

        using var anon = Factory.CreateClient();
        var anonLikedIds = await anon.GetFromJsonAsync<List<int>>("/api/blogposts/liked");
        Assert.Empty(anonLikedIds!);
    }

    [Fact]
    public async Task GetSuggested_ExcludesOwnPostsAndPreferredCategory()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);
        // three posts in the same category establishes it as "preferred"
        await BlogPostTestHelper.CreatePostAsync(Client, categoryId: SeededCategoryId, title: "Mine 1");
        await BlogPostTestHelper.CreatePostAsync(Client, categoryId: SeededCategoryId, title: "Mine 2");
        var ownId = await BlogPostTestHelper.CreatePostAsync(Client, categoryId: SeededCategoryId, title: "Mine 3");

        using var other = Factory.CreateClient();
        // Admin so this post is Published (visible to "suggested") rather
        // than sitting in PendingApproval.
        await TestAuth.SeedAndAuthenticateAdminAsync(Factory.Services, other);
        var othersOutOfBeatId = await BlogPostTestHelper.CreatePostAsync(other, categoryId: OtherSeededCategoryId, title: "Someone else's");

        var response = await Client.GetAsync("/api/blogposts/suggested?take=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        var ids = items!.Select(i => i.GetProperty("id").GetInt32()).ToList();

        Assert.Contains(othersOutOfBeatId, ids);
        Assert.DoesNotContain(ownId, ids);
    }
}
