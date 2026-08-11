using FluentValidation;

namespace UserRegistration.Application.Features.Messages.Commands.SendMessage;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.RecipientId)
            .NotEmpty();

        RuleFor(x => x.Text)
            .MaximumLength(4000);

        // At least one of text / file / voice note has to be there — an
        // empty message isn't something to send.
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Text) || x.AttachmentUrl is not null || x.VoiceNoteUrl is not null)
            .WithMessage("A message needs text, a file, or a voice note.")
            .WithName("Text");
    }
}
