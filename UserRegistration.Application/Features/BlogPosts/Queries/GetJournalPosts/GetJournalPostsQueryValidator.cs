using FluentValidation;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetJournalPosts;

public sealed class GetJournalPostsQueryValidator : AbstractValidator<GetJournalPostsQuery>
{
    public GetJournalPostsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .When(x => x.CategoryId.HasValue);
    }
}
