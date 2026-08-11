using Microsoft.AspNetCore.SignalR;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.Messages;

namespace UserRegistration.Hubs;

public sealed class SignalRMessageNotifier : IRealtimeMessageNotifier
{
    private readonly IHubContext<MessagesHub> _hubContext;

    public SignalRMessageNotifier(IHubContext<MessagesHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyMessageReceivedAsync(Guid recipientId, MessageDto message, CancellationToken cancellationToken = default) =>
        _hubContext.Clients
            .Group(MessagesHub.GroupNameFor(recipientId))
            .SendAsync("ReceiveMessage", message, cancellationToken);

    public Task NotifyThreadReadAsync(Guid senderId, Guid readByUserId, CancellationToken cancellationToken = default) =>
        _hubContext.Clients
            .Group(MessagesHub.GroupNameFor(senderId))
            .SendAsync("ThreadRead", readByUserId, cancellationToken);
}
