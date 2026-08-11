using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using UserRegistration.Application.Common.Interfaces;

namespace UserRegistration.Hubs;

/// <summary>
/// Pure transport + presence — every actual mutation (sending a message,
/// marking a thread read) still goes through the normal REST endpoints
/// (MessagesController), which then push the result out to the relevant
/// connections via IRealtimeMessageNotifier. Keeping the hub this thin means
/// there's exactly one code path (the MediatR handlers) that enforces
/// validation/ownership, whether the caller used REST or a live connection.
/// </summary>
[Authorize]
public sealed class MessagesHub : Hub
{
    private readonly IUserPresenceTracker _presenceTracker;

    public MessagesHub(IUserPresenceTracker presenceTracker)
    {
        _presenceTracker = presenceTracker;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId is { } id)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupNameFor(id));

            var justCameOnline = _presenceTracker.AddConnection(id, Context.ConnectionId);
            if (justCameOnline)
            {
                await Clients.Others.SendAsync("UserOnline", id);
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId is { } id)
        {
            var wentOffline = _presenceTracker.RemoveConnection(id, Context.ConnectionId);
            if (wentOffline)
            {
                await Clients.Others.SendAsync("UserOffline", id);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Who's online right now — sent back to the caller alone,
    /// right after they connect, so the UI doesn't have to wait on a
    /// separate REST round trip just to paint the initial presence state.</summary>
    public Task<Guid[]> GetOnlineUserIds() => Task.FromResult(_presenceTracker.GetOnlineUserIds().ToArray());

    internal static string GroupNameFor(Guid userId) => $"user-{userId:N}";

    private Guid? GetUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
