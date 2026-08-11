using MediatR;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.Messages;
using UserRegistration.Domain.Entities;

namespace UserRegistration.Application.Features.Messages.Commands.SendMessage;

public sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, MessageDto>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRealtimeMessageNotifier _realtimeNotifier;

    public SendMessageCommandHandler(
        IMessageRepository messageRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IRealtimeMessageNotifier realtimeNotifier)
    {
        _messageRepository = messageRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<MessageDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var senderId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to send a message.");

        var sender = await _userRepository.GetByIdAsync(senderId, cancellationToken)
            ?? throw new UnauthorizedException("Your account could not be found.");

        _ = await _userRepository.GetByIdAsync(request.RecipientId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.RecipientId);

        var message = new Message
        {
            SenderId = senderId,
            RecipientId = request.RecipientId,
            Text = string.IsNullOrWhiteSpace(request.Text) ? null : request.Text.Trim(),
            AttachmentUrl = request.AttachmentUrl,
            AttachmentFileName = request.AttachmentFileName,
            AttachmentContentType = request.AttachmentContentType,
            AttachmentSizeBytes = request.AttachmentSizeBytes,
            VoiceNoteUrl = request.VoiceNoteUrl,
            VoiceNoteDurationSeconds = request.VoiceNoteDurationSeconds,
            IsRead = false,
            CreatedDate = DateTime.UtcNow,
        };

        await _messageRepository.AddAsync(message, cancellationToken);
        await _messageRepository.SaveChangesAsync(cancellationToken);

        var dto = new MessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderName = sender.FullName,
            SenderProfileImagePath = sender.ProfileImagePath,
            RecipientId = message.RecipientId,
            Text = message.Text,
            AttachmentUrl = message.AttachmentUrl,
            AttachmentFileName = message.AttachmentFileName,
            AttachmentContentType = message.AttachmentContentType,
            AttachmentSizeBytes = message.AttachmentSizeBytes,
            VoiceNoteUrl = message.VoiceNoteUrl,
            VoiceNoteDurationSeconds = message.VoiceNoteDurationSeconds,
            IsRead = message.IsRead,
            CreatedDate = message.CreatedDate,
        };

        // Best-effort — a recipient who isn't currently connected just picks
        // the message up next time they load their conversations/thread.
        await _realtimeNotifier.NotifyMessageReceivedAsync(request.RecipientId, dto, cancellationToken);

        return dto;
    }
}
