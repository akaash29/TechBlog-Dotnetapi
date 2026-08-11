using UserRegistration.Application.DTOs.Messages;
using UserRegistration.Domain.Entities;

namespace UserRegistration.Application.Common.Interfaces;

public interface IMessageRepository
{
    Task AddAsync(Message message, CancellationToken cancellationToken = default);

    /// <summary>Every conversation the user is part of, newest first — one row
    /// per other participant, with a preview of their last exchange.</summary>
    Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>The most recent messages between two users, oldest first (reading order).</summary>
    Task<IReadOnlyList<MessageDto>> GetThreadAsync(
        Guid userId, Guid otherUserId, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>Marks every message the other user sent to the caller as read.</summary>
    Task MarkThreadAsReadAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
