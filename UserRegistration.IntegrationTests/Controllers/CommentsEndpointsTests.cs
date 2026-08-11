using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UserRegistration.IntegrationTests.Infrastructure;

namespace UserRegistration.IntegrationTests.Controllers;

public sealed class CommentsEndpointsTests : IntegrationTestBase
{
    public CommentsEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Add_ThenGetByPost_ReturnsCommentAndIncrementsCommentsCount()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);
        var postId = await BlogPostTestHelper.CreatePostAsync(Client);

        var addResponse = await Client.PostAsJsonAsync("/api/comments", new
        {
            BlogPostId = postId,
            CommentText = "Great read."
        });
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        var getResponse = await Client.GetAsync($"/api/comments?blogPostId={postId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var comments = await getResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.Single(comments!);
        Assert.Equal("Great read.", comments![0].GetProperty("commentText").GetString());

        var post = await Client.GetFromJsonAsync<JsonElement>($"/api/blogposts/{postId}");
        Assert.Equal(1, post.GetProperty("commentsCount").GetInt32());
    }

    [Fact]
    public async Task Add_WithoutAuthentication_ReturnsUnauthorized()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);
        var postId = await BlogPostTestHelper.CreatePostAsync(Client);

        using var anon = Factory.CreateClient();
        var response = await anon.PostAsJsonAsync("/api/comments", new { BlogPostId = postId, CommentText = "Hi" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Add_WithEmptyText_ReturnsBadRequest()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);
        var postId = await BlogPostTestHelper.CreatePostAsync(Client);

        var response = await Client.PostAsJsonAsync("/api/comments", new { BlogPostId = postId, CommentText = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_AsOwner_ReturnsOkWithNewText()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);
        var postId = await BlogPostTestHelper.CreatePostAsync(Client);
        var commentId = await AddCommentAsync(Client, postId, "Original text");

        var response = await Client.PutAsJsonAsync($"/api/comments/{commentId}", new { CommentText = "Edited text" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Edited text", body.GetProperty("commentText").GetString());
    }

    [Fact]
    public async Task Update_AsNonOwner_ReturnsForbidden()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);
        var postId = await BlogPostTestHelper.CreatePostAsync(Client);
        var commentId = await AddCommentAsync(Client, postId, "Original text");

        using var other = Factory.CreateClient();
        await TestAuth.RegisterAndAuthenticateAsync(other);

        var response = await other.PutAsJsonAsync($"/api/comments/{commentId}", new { CommentText = "Hijacked" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AsOwner_ReturnsNoContentAndDecrementsCommentsCount()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);
        var postId = await BlogPostTestHelper.CreatePostAsync(Client);
        var commentId = await AddCommentAsync(Client, postId, "Delete me");

        var response = await Client.DeleteAsync($"/api/comments/{commentId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var post = await Client.GetFromJsonAsync<JsonElement>($"/api/blogposts/{postId}");
        Assert.Equal(0, post.GetProperty("commentsCount").GetInt32());

        var getResponse = await Client.GetAsync($"/api/comments?blogPostId={postId}");
        var comments = await getResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.Empty(comments!);
    }

    [Fact]
    public async Task Delete_AsNonOwner_ReturnsForbidden()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);
        var postId = await BlogPostTestHelper.CreatePostAsync(Client);
        var commentId = await AddCommentAsync(Client, postId, "Keep me");

        using var other = Factory.CreateClient();
        await TestAuth.RegisterAndAuthenticateAsync(other);

        var response = await other.DeleteAsync($"/api/comments/{commentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<int> AddCommentAsync(HttpClient client, int postId, string text)
    {
        var response = await client.PostAsJsonAsync("/api/comments", new { BlogPostId = postId, CommentText = text });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }
}
