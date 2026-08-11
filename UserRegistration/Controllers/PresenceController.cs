using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserRegistration.Application.Common.Interfaces;

namespace UserRegistration.Controllers;

/// <summary>Who's online right now — the initial paint for the Members page's
/// status dots before a live SignalR connection is (or if it never gets)
/// established; the hub itself is the source of truth once connected.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public sealed class PresenceController : ControllerBase
{
    private readonly IUserPresenceTracker _presenceTracker;

    public PresenceController(IUserPresenceTracker presenceTracker)
    {
        _presenceTracker = presenceTracker;
    }

    [HttpGet("online")]
    [ProducesResponseType(typeof(IReadOnlyList<Guid>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<Guid>> GetOnline() =>
        Ok(_presenceTracker.GetOnlineUserIds());
}
