using FluentValidation;
using UserRegistration.Application.Common.Constants;

namespace UserRegistration.Application.Features.Messages.Commands.UploadMessageAttachment;

public sealed class UploadMessageAttachmentCommandValidator : AbstractValidator<UploadMessageAttachmentCommand>
{
    private static readonly string[] AllowedKinds = ["file", "voice"];

    public UploadMessageAttachmentCommandValidator()
    {
        RuleFor(x => x.RecipientId)
            .NotEmpty();

        RuleFor(x => x.FileName)
            .NotEmpty();

        RuleFor(x => x.Kind)
            .Must(kind => AllowedKinds.Contains(kind, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Kind must be 'file' or 'voice'.");

        RuleFor(x => x.Length)
            .GreaterThan(0)
            .WithMessage("The file is empty.")
            .LessThanOrEqualTo(MessageAttachmentConstraints.MaxSizeBytes)
            .WithMessage($"Attachments must be {MessageAttachmentConstraints.MaxSizeBytes / 1024 / 1024} MB or smaller.");

        RuleFor(x => x.ContentType)
            .Must((command, contentType) => AllowedContentTypesFor(command.Kind).Contains(contentType, StringComparer.OrdinalIgnoreCase))
            .WithMessage("That file type isn't supported.");
    }

    private static string[] AllowedContentTypesFor(string kind) =>
        string.Equals(kind, "voice", StringComparison.OrdinalIgnoreCase)
            ? MessageAttachmentConstraints.AllowedVoiceContentTypes
            : MessageAttachmentConstraints.AllowedFileContentTypes;
}
