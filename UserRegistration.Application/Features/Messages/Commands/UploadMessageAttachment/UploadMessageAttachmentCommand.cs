using MediatR;
using UserRegistration.Application.DTOs.Messages;

namespace UserRegistration.Application.Features.Messages.Commands.UploadMessageAttachment;

public sealed class UploadMessageAttachmentCommand : IRequest<UploadMessageAttachmentResponse>
{
    public required Guid RecipientId { get; init; }

    public required Stream Content { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public required long Length { get; init; }

    /// <summary>"file" or "voice" — picks the validation rules (allowed content
    /// types) and the blob's virtual subfolder.</summary>
    public required string Kind { get; init; }
}
