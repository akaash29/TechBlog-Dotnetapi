namespace UserRegistration.Domain.Entities;

/// <summary>
/// One page visit, tracked by the Angular app on every route change (see
/// AnalyticsController.Track) and updated by periodic heartbeats while the
/// visitor stays on the page. Powers the admin Insights dashboard — not
/// linked to BaseEntity/AuditableEntity, since "who created this row" isn't
/// a meaningful concept here the way it is for content tables.
/// </summary>
public class PageView
{
    public long Id { get; set; }

    /// <summary>A client-generated id (persisted in localStorage) identifying
    /// one browser across visits — the closest thing to "a visitor" without
    /// requiring a login.</summary>
    public string SessionId { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public User? User { get; set; }

    public string Path { get; set; } = string.Empty;

    /// <summary>Resolved from IpAddress via IGeoLocationService; null for
    /// local/private addresses or if the lookup fails.</summary>
    public string? Country { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    /// <summary>Desktop / Mobile / Tablet / Bot / Unknown — sniffed from UserAgent once at track time.</summary>
    public string DeviceType { get; set; } = "Unknown";

    public DateTime VisitedAt { get; set; }

    /// <summary>Bumped by each heartbeat — "active now" is derived from this, not VisitedAt.</summary>
    public DateTime LastActivityAt { get; set; }

    public int DurationSeconds { get; set; }
}
