namespace UserRegistration.Application.DTOs.Analytics;

/// <summary>What the client actually sends — IP/UserAgent/UserId are filled
/// in by the controller from the request itself, never trusted from the body.</summary>
public sealed class TrackPageViewRequest
{
    public string SessionId { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;
}

public sealed class TrackPageViewResponse
{
    public long Id { get; set; }
}

public sealed class RecordHeartbeatRequest
{
    public long PageViewId { get; set; }

    public int ElapsedSeconds { get; set; }
}
