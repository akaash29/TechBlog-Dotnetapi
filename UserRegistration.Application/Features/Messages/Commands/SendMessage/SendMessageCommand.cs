using MediatR;
using UserRegistration.Application.DTOs.Messages;

namespace UserRegistration.Application.Features.Messages.Commands.SendMessage;

public sealed class SendMessageCommand : IRequest<MessageDto>
{
    public Guid RecipientId { get; set; }

    public string? Text { get; set; }

    public string? AttachmentUrl { get; set; }

    public string? AttachmentFileName { get; set; }

    public string? AttachmentContentType { get; set; }

    public long? AttachmentSizeBytes { get; set; }

    public string? VoiceNoteUrl { get; set; }

    public int? VoiceNoteDurationSeconds { get; set; }
}
