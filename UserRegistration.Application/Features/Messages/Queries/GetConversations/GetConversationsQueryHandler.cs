using MediatR;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.Messages;

namespace UserRegistration.Application.Features.Messages.Queries.GetConversations;

public sealed class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, IReadOnlyList<ConversationDto>>
{
    private readonly IMessageRepository _messageRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserPresenceTracker _presenceTracker;

    public GetConversationsQueryHandler(
        IMessageRepository messageRepository,
        ICurrentUserService currentUserService,
        IUserPresenceTracker presenceTracker)
    {
        _messageRepository = messageRepository;
        _currentUserService = currentUserService;
        _presenceTracker = presenceTracker;
    }

    public async Task<IReadOnlyList<ConversationDto>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view your messages.");

        var conversations = await _messageRepository.GetConversationsAsync(userId, cancellationToken);

        foreach (var conversation in conversations)
        {
            conversation.IsOnline = _presenceTracker.IsOnline(conversation.OtherUserId);
        }

        return conversations;
    }
}
