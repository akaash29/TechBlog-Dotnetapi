using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserRegistration.Application.DTOs.Analytics;
using UserRegistration.Application.Features.Analytics.GetAnalyticsSummary;
using UserRegistration.Application.Features.Analytics.RecordHeartbeat;
using UserRegistration.Application.Features.Analytics.TrackPageView;
using UserRegistration.Domain.Enums;

namespace UserRegistration.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly ISender _sender;

    public AnalyticsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Records one page view. Called by the Angular app on every route
    /// change, signed in or not — IP/UA/user id come from the request itself,
    /// never the body, so a caller can't spoof them.</summary>
    [AllowAnonymous]
    [HttpPost("track")]
    [ProducesResponseType(typeof(TrackPageViewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrackPageViewResponse>> Track(
        [FromBody] TrackPageViewRequest request,
        CancellationToken cancellationToken)
    {
        var command = new TrackPageViewCommand
        {
            SessionId = request.SessionId,
            Path = request.Path,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            UserId = TryGetUserId()
        };

        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Keeps a tracked page view's "last active" time and duration
    /// current — sent periodically while a tab stays open, and once more via
    /// sendBeacon when it closes.</summary>
    [AllowAnonymous]
    [HttpPost("heartbeat")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Heartbeat(
        [FromBody] RecordHeartbeatRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new RecordHeartbeatCommand { PageViewId = request.PageViewId, ElapsedSeconds = request.ElapsedSeconds },
            cancellationToken);

        return NoContent();
    }

    /// <summary>Traffic summary for the admin Insights dashboard.</summary>
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("summary")]
    [ProducesResponseType(typeof(AnalyticsSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnalyticsSummaryDto>> GetSummary(
        [FromQuery] string range = "week",
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetAnalyticsSummaryQuery { Range = range }, cancellationToken);
        return Ok(result);
    }

    private Guid? TryGetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
