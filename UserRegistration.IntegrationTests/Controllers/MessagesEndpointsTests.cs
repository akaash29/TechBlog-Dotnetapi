using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using UserRegistration.IntegrationTests.Infrastructure;

namespace UserRegistration.IntegrationTests.Controllers;

public sealed class MessagesEndpointsTests : IntegrationTestBase
{
    public MessagesEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Send_ThenGetThread_ReturnsTheMessage()
    {
        var me = await TestAuth.RegisterAndAuthenticateAsync(Client);
        var meId = await GetUserIdByEmailAsync(Client, me.Email);

        using var other = Factory.CreateClient();
        var otherAccount = await TestAuth.RegisterAndAuthenticateAsync(other);
        var otherId = await GetUserIdByEmailAsync(other, otherAccount.Email);

        var response = await Client.PostAsJsonAsync("/api/messages", new { RecipientId = otherId, Text = "Hey there" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var threadResponse = await Client.GetAsync($"/api/messages/thread/{otherId}");
        Assert.Equal(HttpStatusCode.OK, threadResponse.StatusCode);
        var messages = await threadResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.Single(messages!);
        Assert.Equal("Hey there", messages![0].GetProperty("text").GetString());
        Assert.Equal(meId, messages[0].GetProperty("senderId").GetGuid());
    }

    [Fact]
    public async Task Send_WithNoContentAtAll_ReturnsBadRequest()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        using var other = Factory.CreateClient();
        var otherAccount = await TestAuth.RegisterAndAuthenticateAsync(other);
        var otherId = await GetUserIdByEmailAsync(other, otherAccount.Email);

        var response = await Client.PostAsJsonAsync("/api/messages", new { RecipientId = otherId, Text = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Send_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/messages", new { RecipientId = Guid.NewGuid(), Text = "Hi" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetConversations_ReflectsLastMessageAndUnreadCount()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        using var other = Factory.CreateClient();
        var otherAccount = await TestAuth.RegisterAndAuthenticateAsync(other);
        var otherId = await GetUserIdByEmailAsync(other, otherAccount.Email);

        await Client.PostAsJsonAsync("/api/messages", new { RecipientId = otherId, Text = "First" });
        await Client.PostAsJsonAsync("/api/messages", new { RecipientId = otherId, Text = "Second" });

        var response = await other.GetAsync("/api/messages/conversations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var conversations = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.Single(conversations!);
        Assert.Equal("Second", conversations![0].GetProperty("lastMessagePreview").GetString());
        Assert.Equal(2, conversations[0].GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public async Task MarkThreadRead_ZeroesOutUnreadCount()
    {
        var me = await TestAuth.RegisterAndAuthenticateAsync(Client);
        var meId = await GetUserIdByEmailAsync(Client, me.Email);

        using var other = Factory.CreateClient();
        var otherAccount = await TestAuth.RegisterAndAuthenticateAsync(other);
        var otherId = await GetUserIdByEmailAsync(other, otherAccount.Email);

        await Client.PostAsJsonAsync("/api/messages", new { RecipientId = otherId, Text = "Ping" });

        var markReadResponse = await other.PostAsync($"/api/messages/thread/{meId}/read", content: null);
        Assert.Equal(HttpStatusCode.NoContent, markReadResponse.StatusCode);

        var unreadResponse = await other.GetFromJsonAsync<int>("/api/messages/unread-count");
        Assert.Equal(0, unreadResponse);
    }

    [Fact]
    public async Task UploadAttachment_OverSizeLimit_ReturnsBadRequest()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        using var other = Factory.CreateClient();
        var otherAccount = await TestAuth.RegisterAndAuthenticateAsync(other);
        var otherId = await GetUserIdByEmailAsync(other, otherAccount.Email);

        // 5 MB limit + 100KB over — under the controller's request-size ceiling
        // (5 MB + 1 MB headroom), exercising the validator, not Kestrel's cutoff.
        const long maxSizeBytes = 5 * 1024 * 1024;
        using var content = BuildAttachmentContent(new byte[maxSizeBytes + 100_000], "image/jpeg", "big.jpg", otherId, "file");

        var response = await Client.PostAsync("/api/messages/attachments", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadAttachment_WithDisallowedContentType_ReturnsBadRequest()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        using var other = Factory.CreateClient();
        var otherAccount = await TestAuth.RegisterAndAuthenticateAsync(other);
        var otherId = await GetUserIdByEmailAsync(other, otherAccount.Email);

        using var content = BuildAttachmentContent(new byte[1024], "application/x-msdownload", "app.exe", otherId, "file");

        var response = await Client.PostAsync("/api/messages/attachments", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadAttachment_ThenSendVoiceNote_ReturnsCreated()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        using var other = Factory.CreateClient();
        var otherAccount = await TestAuth.RegisterAndAuthenticateAsync(other);
        var otherId = await GetUserIdByEmailAsync(other, otherAccount.Email);

        using var content = BuildAttachmentContent(new byte[2048], "audio/webm", "note.webm", otherId, "voice");
        var uploadResponse = await Client.PostAsync("/api/messages/attachments", content);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
        var url = uploaded.GetProperty("url").GetString();

        var sendResponse = await Client.PostAsJsonAsync("/api/messages", new
        {
            RecipientId = otherId,
            VoiceNoteUrl = url,
            VoiceNoteDurationSeconds = 12
        });

        Assert.Equal(HttpStatusCode.Created, sendResponse.StatusCode);
    }

    [Fact]
    public async Task GetOnlinePresence_ReturnsOkForAuthenticatedCaller()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        var response = await Client.GetAsync("/api/presence/online");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static MultipartFormDataContent BuildAttachmentContent(
        byte[] bytes, string contentType, string fileName, Guid recipientId, string kind)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        content.Add(new StringContent(recipientId.ToString()), "recipientId");
        content.Add(new StringContent(kind), "kind");
        return content;
    }

    private static async Task<Guid> GetUserIdByEmailAsync(HttpClient client, string email)
    {
        var response = await client.GetAsync($"/api/users/by-email?email={Uri.EscapeDataString(email)}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }
}
