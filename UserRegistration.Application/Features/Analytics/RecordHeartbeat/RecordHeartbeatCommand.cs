using MediatR;

namespace UserRegistration.Application.Features.Analytics.RecordHeartbeat;

public sealed class RecordHeartbeatCommand : IRequest
{
    public long PageViewId { get; set; }

    public int ElapsedSeconds { get; set; }
}
