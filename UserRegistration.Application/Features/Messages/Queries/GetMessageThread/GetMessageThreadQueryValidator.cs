using FluentValidation;

namespace UserRegistration.Application.Features.Messages.Queries.GetMessageThread;

public sealed class GetMessageThreadQueryValidator : AbstractValidator<GetMessageThreadQuery>
{
    public GetMessageThreadQueryValidator()
    {
        RuleFor(x => x.OtherUserId)
            .NotEmpty();

        RuleFor(x => x.Take)
            .InclusiveBetween(1, 200);
    }
}
