using System.Net;
using System.Net.Http.Json;
using UserRegistration.IntegrationTests.Infrastructure;

namespace UserRegistration.IntegrationTests.Controllers;

public sealed class AnalyticsEndpointsTests : IntegrationTestBase
{
    public AnalyticsEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Track_WithoutAuthentication_ReturnsOkWithAnId()
    {
        var response = await Client.PostAsJsonAsync("/api/analytics/track", new
        {
            SessionId = Guid.NewGuid().ToString(),
            Path = "/feed"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TrackResponseBody>();
        Assert.True(body!.Id > 0);
    }

    [Fact]
    public async Task Track_WithEmptySessionId_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/analytics/track", new
        {
            SessionId = "",
            Path = "/feed"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Heartbeat_ForATrackedPageView_ReturnsNoContent()
    {
        var trackResponse = await Client.PostAsJsonAsync("/api/analytics/track", new
        {
            SessionId = Guid.NewGuid().ToString(),
            Path = "/feed"
        });
        var tracked = await trackResponse.Content.ReadFromJsonAsync<TrackResponseBody>();

        var response = await Client.PostAsJsonAsync("/api/analytics/heartbeat", new
        {
            PageViewId = tracked!.Id,
            ElapsedSeconds = 12
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Heartbeat_ForAnUnknownPageView_StillReturnsNoContent()
    {
        // Best-effort background beacon — a stale/unknown id shouldn't error.
        var response = await Client.PostAsJsonAsync("/api/analytics/heartbeat", new
        {
            PageViewId = 999_999_999L,
            ElapsedSeconds = 5
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_AsAdmin_ReturnsOk()
    {
        await TestAuth.SeedAndAuthenticateAdminAsync(Factory.Services, Client);

        var response = await Client.GetAsync("/api/analytics/summary?range=week");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_AsNonAdmin_ReturnsForbidden()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        var response = await Client.GetAsync("/api/analytics/summary?range=week");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/analytics/summary?range=week");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class TrackResponseBody
    {
        public long Id { get; set; }
    }
}
