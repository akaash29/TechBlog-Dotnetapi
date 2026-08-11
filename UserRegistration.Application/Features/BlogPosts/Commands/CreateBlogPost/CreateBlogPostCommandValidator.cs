using FluentValidation;

namespace UserRegistration.Application.Features.BlogPosts.Commands.CreateBlogPost;

public sealed class CreateBlogPostCommandValidator : AbstractValidator<CreateBlogPostCommand>
{
    public CreateBlogPostCommandValidator()
    {
        // A category is always required, draft or not — it's one dropdown, low friction,
        // and CategoryId isn't nullable on the entity.
        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("Choose a category.");

        RuleFor(x => x.Title).MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(500);

        // Drafts can be saved incomplete — only publishing needs the full set.
        When(x => !x.IsDraft, () =>
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Add a headline before publishing.");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Add a standfirst before publishing.");

            RuleFor(x => x.PostHtml)
                .NotEmpty()
                .WithMessage("Write something before publishing.");
        });
    }
}
