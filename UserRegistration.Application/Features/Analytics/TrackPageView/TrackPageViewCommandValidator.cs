using FluentValidation;

namespace UserRegistration.Application.Features.Analytics.TrackPageView;

public sealed class TrackPageViewCommandValidator : AbstractValidator<TrackPageViewCommand>
{
    public TrackPageViewCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Path).NotEmpty().MaximumLength(500);
    }
}
