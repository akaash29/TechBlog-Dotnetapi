using MediatR;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.Messages;

namespace UserRegistration.Application.Features.Messages.Queries.GetMessageThread;

public sealed class GetMessageThreadQueryHandler : IRequestHandler<GetMessageThreadQuery, IReadOnlyList<MessageDto>>
{
    private readonly IMessageRepository _messageRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMessageThreadQueryHandler(IMessageRepository messageRepository, ICurrentUserService currentUserService)
    {
        _messageRepository = messageRepository;
        _currentUserService = currentUserService;
    }

    public Task<IReadOnlyList<MessageDto>> Handle(GetMessageThreadQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view your messages.");

        return _messageRepository.GetThreadAsync(userId, request.OtherUserId, request.Take, cancellationToken);
    }
}
