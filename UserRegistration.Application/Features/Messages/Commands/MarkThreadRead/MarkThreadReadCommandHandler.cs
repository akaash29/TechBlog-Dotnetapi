using MediatR;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;

namespace UserRegistration.Application.Features.Messages.Commands.MarkThreadRead;

public sealed class MarkThreadReadCommandHandler : IRequestHandler<MarkThreadReadCommand>
{
    private readonly IMessageRepository _messageRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRealtimeMessageNotifier _realtimeNotifier;

    public MarkThreadReadCommandHandler(
        IMessageRepository messageRepository,
        ICurrentUserService currentUserService,
        IRealtimeMessageNotifier realtimeNotifier)
    {
        _messageRepository = messageRepository;
        _currentUserService = currentUserService;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task Handle(MarkThreadReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to read your messages.");

        await _messageRepository.MarkThreadAsReadAsync(userId, request.OtherUserId, cancellationToken);
        await _realtimeNotifier.NotifyThreadReadAsync(request.OtherUserId, userId, cancellationToken);
    }
}
