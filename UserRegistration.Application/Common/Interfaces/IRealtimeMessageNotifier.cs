using UserRegistration.Application.DTOs.Messages;

namespace UserRegistration.Application.Common.Interfaces;

/// <summary>Pushes a just-sent message to the recipient's live connection(s),
/// if any — implemented against SignalR in the API project (where the hub
/// lives); the Application layer only needs to know "notify this user",
/// not the transport.</summary>
public interface IRealtimeMessageNotifier
{
    Task NotifyMessageReceivedAsync(Guid recipientId, MessageDto message, CancellationToken cancellationToken = default);

    Task NotifyThreadReadAsync(Guid senderId, Guid readByUserId, CancellationToken cancellationToken = default);
}
