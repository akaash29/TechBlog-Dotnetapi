using System.Collections.Concurrent;
using UserRegistration.Application.Common.Interfaces;

namespace UserRegistration.Infrastructure.Realtime;

/// <summary>
/// A single process's view of who's connected — fine for one API instance
/// (this app's deployment target). Scaling to multiple instances would need
/// a shared backing store (e.g. Redis) behind the same interface; nothing
/// above this class would need to change.
/// </summary>
public sealed class InMemoryUserPresenceTracker : IUserPresenceTracker
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _connectionsByUser = new();

    public bool AddConnection(Guid userId, string connectionId)
    {
        var connections = _connectionsByUser.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
        var wasEmpty = connections.IsEmpty;
        connections[connectionId] = 0;
        return wasEmpty;
    }

    public bool RemoveConnection(Guid userId, string connectionId)
    {
        if (!_connectionsByUser.TryGetValue(userId, out var connections))
        {
            return false;
        }

        connections.TryRemove(connectionId, out _);
        if (!connections.IsEmpty)
        {
            return false;
        }

        // Only drop the outer entry if it's still empty — a reconnect racing
        // in between the emptiness check and this removal just means the
        // TryRemove below no-ops on a dictionary that already has an entry.
        _connectionsByUser.TryRemove(new KeyValuePair<Guid, ConcurrentDictionary<string, byte>>(userId, connections));
        return true;
    }

    public bool IsOnline(Guid userId) =>
        _connectionsByUser.TryGetValue(userId, out var connections) && !connections.IsEmpty;

    public IReadOnlySet<Guid> GetOnlineUserIds() =>
        _connectionsByUser.Where(kvp => !kvp.Value.IsEmpty).Select(kvp => kvp.Key).ToHashSet();
}
