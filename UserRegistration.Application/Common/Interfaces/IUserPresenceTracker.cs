namespace UserRegistration.Application.Common.Interfaces;

/// <summary>
/// Tracks which users currently have a live SignalR connection — the source
/// of truth for "online now" on the Members page and in Messages. A user
/// can have more than one connection open at once (multiple tabs/devices),
/// so this is a ref-count per user, not a flag: they stay "online" until
/// the *last* connection drops.
/// </summary>
public interface IUserPresenceTracker
{
    /// <returns>True if this is the user's first open connection (they just went online).</returns>
    bool AddConnection(Guid userId, string connectionId);

    /// <returns>True if that was the user's last open connection (they just went offline).</returns>
    bool RemoveConnection(Guid userId, string connectionId);

    bool IsOnline(Guid userId);

    IReadOnlySet<Guid> GetOnlineUserIds();
}
