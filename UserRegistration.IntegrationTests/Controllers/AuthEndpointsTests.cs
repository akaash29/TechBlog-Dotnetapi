using System.Net;
using System.Net.Http.Json;
using UserRegistration.IntegrationTests.Infrastructure;

namespace UserRegistration.IntegrationTests.Controllers;

public sealed class AuthEndpointsTests : IntegrationTestBase
{
    public AuthEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsTokenPair()
    {
        var account = await TestAuth.RegisterAsync(Client);

        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            EmailOrUserName = account.Email,
            Password = account.Password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseBody>();
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var account = await TestAuth.RegisterAsync(Client);

        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            EmailOrUserName = account.Email,
            Password = "TheWrongPassword1"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownUser_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            EmailOrUserName = $"nobody{Guid.NewGuid():N}@test.local",
            Password = "Passw0rd1"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidToken_RotatesAndReturnsNewTokenPair()
    {
        var account = await TestAuth.RegisterAsync(Client);
        var original = await TestAuth.LoginAsync(Client, account);

        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = original.RefreshToken });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseBody>();
        Assert.NotEqual(original.RefreshToken, body!.RefreshToken);
        Assert.NotEqual(original.AccessToken, body.AccessToken);
    }

    [Fact]
    public async Task Refresh_WithAlreadyUsedToken_ReturnsUnauthorized()
    {
        var account = await TestAuth.RegisterAsync(Client);
        var original = await TestAuth.LoginAsync(Client, account);

        // spend it once — rotation revokes the original
        await Client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = original.RefreshToken });

        var replay = await Client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = original.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = "not-a-real-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Revoke_WithValidToken_ReturnsNoContentAndInvalidatesIt()
    {
        var account = await TestAuth.RegisterAsync(Client);
        var auth = await TestAuth.LoginAsync(Client, account);

        var revokeResponse = await Client.PostAsJsonAsync("/api/auth/revoke", new { RefreshToken = auth.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        var reuseResponse = await Client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = auth.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task Revoke_WithUnknownToken_ReturnsNotFound()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/revoke", new { RefreshToken = "not-a-real-token" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
