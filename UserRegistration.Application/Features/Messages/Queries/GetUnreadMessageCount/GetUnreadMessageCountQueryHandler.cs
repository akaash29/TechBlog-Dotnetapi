using MediatR;
using UserRegistration.Application.Common.Interfaces;

namespace UserRegistration.Application.Features.Messages.Queries.GetUnreadMessageCount;

public sealed class GetUnreadMessageCountQueryHandler : IRequestHandler<GetUnreadMessageCountQuery, int>
{
    private readonly IMessageRepository _messageRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetUnreadMessageCountQueryHandler(IMessageRepository messageRepository, ICurrentUserService currentUserService)
    {
        _messageRepository = messageRepository;
        _currentUserService = currentUserService;
    }

    public Task<int> Handle(GetUnreadMessageCountQuery request, CancellationToken cancellationToken) =>
        _currentUserService.UserId is { } userId
            ? _messageRepository.GetUnreadCountAsync(userId, cancellationToken)
            : Task.FromResult(0);
}
