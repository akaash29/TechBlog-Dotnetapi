using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.IntegrationTests.Infrastructure;

namespace UserRegistration.IntegrationTests.Controllers;

public sealed class ImagesEndpointsTests : IntegrationTestBase
{
    // matches Application.Common.Constants.ImageUploadConstraints.MaxSizeBytes
    private const long MaxSizeBytes = 10 * 1024 * 1024;

    public ImagesEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Upload_WhenAuthenticated_ReturnsUploadedImage()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        using var content = BuildMultipartContent(new byte[1024], "image/jpeg", "cover.jpg");
        var response = await Client.PostAsync("/api/images/upload", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UploadResponseBody>();
        Assert.False(string.IsNullOrWhiteSpace(body!.ImagePath));
    }

    [Fact]
    public async Task Upload_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var content = BuildMultipartContent(new byte[1024], "image/jpeg", "cover.jpg");

        var response = await Client.PostAsync("/api/images/upload", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upload_OverTheSizeLimit_ReturnsBadRequest()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        // Over the 10 MB business rule but under the controller's request-size
        // ceiling (10 MB + 1 MB headroom) — exercises the validator, not Kestrel's cutoff.
        using var content = BuildMultipartContent(new byte[MaxSizeBytes + 100_000], "image/jpeg", "too-big.jpg");

        var response = await Client.PostAsync("/api/images/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithDisallowedContentType_ReturnsBadRequest()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        using var content = BuildMultipartContent(new byte[1024], "text/plain", "notes.txt");

        var response = await Client.PostAsync("/api/images/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithNoFile_ReturnsBadRequest()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);
        using var content = new MultipartFormDataContent();

        var response = await Client.PostAsync("/api/images/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DiscardOrphaned_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await Client.PostAsync("/api/images/discard-orphaned", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DiscardOrphaned_RemovesNeverSavedUploads_ButNotOnesLinkedToAPost()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        using var orphanContent = BuildMultipartContent(new byte[1024], "image/jpeg", "orphan.jpg");
        var orphanUpload = await (await Client.PostAsync("/api/images/upload", orphanContent))
            .Content.ReadFromJsonAsync<UploadResponseBody>();

        using var coverContent = BuildMultipartContent(new byte[1024], "image/jpeg", "cover.jpg");
        var coverUpload = await (await Client.PostAsync("/api/images/upload", coverContent))
            .Content.ReadFromJsonAsync<UploadResponseBody>();

        // Saving a post that actually uses coverUpload's path links it —
        // LinkOrphanedImagesAsync (see CreateBlogPostCommandHandler) — so
        // it should survive the discard even though it was uploaded in the
        // very same "session" as the still-orphaned one.
        await Client.PostAsJsonAsync("/api/blogposts", new
        {
            Title = "Uses the cover",
            Description = "Standfirst.",
            PostHtml = "<p>Body</p>",
            CoverImagePath = coverUpload!.ImagePath,
            CategoryId = 1,
            IsDraft = true
        });

        var discardResponse = await Client.PostAsync("/api/images/discard-orphaned", content: null);
        Assert.Equal(HttpStatusCode.NoContent, discardResponse.StatusCode);

        var fakeBlobStorage = (FakeBlobStorageService)Factory.Services.GetRequiredService<IBlobStorageService>();
        Assert.Contains(orphanUpload!.ImagePath, fakeBlobStorage.DeletedUrls);
        Assert.DoesNotContain(coverUpload!.ImagePath, fakeBlobStorage.DeletedUrls);
    }

    private static MultipartFormDataContent BuildMultipartContent(byte[] bytes, string contentType, string fileName)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        return content;
    }

    private sealed class UploadResponseBody
    {
        public int Id { get; set; }
        public string ImagePath { get; set; } = string.Empty;
    }
}
