using System.Net.Http.Json;
using System.Text.Json;

namespace UserRegistration.IntegrationTests.Infrastructure;

/// <summary>Creates posts through the real API (not straight into the DB) so
/// every test exercises the same path a real writer would — the client
/// passed in must already be authenticated (see TestAuth).</summary>
public static class BlogPostTestHelper
{
    public static async Task<int> CreatePostAsync(
        HttpClient client, int categoryId = 1, bool isDraft = false, string? title = null)
    {
        var response = await client.PostAsJsonAsync("/api/blogposts", new
        {
            Title = title ?? $"Post {Guid.NewGuid():N}",
            Description = "A standfirst.",
            PostHtml = "<p>Body</p>",
            CoverImagePath = (string?)null,
            CategoryId = categoryId,
            IsDraft = isDraft
        });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }
}
