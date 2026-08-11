using FluentValidation;

namespace UserRegistration.Application.Features.Comments.Commands.AddComment;

public sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.BlogPostId)
            .GreaterThan(0);

        RuleFor(x => x.CommentText)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
