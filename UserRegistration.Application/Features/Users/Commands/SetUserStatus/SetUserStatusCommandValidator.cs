using FluentValidation;

namespace UserRegistration.Application.Features.Users.Commands.SetUserStatus;

public sealed class SetUserStatusCommandValidator : AbstractValidator<SetUserStatusCommand>
{
    public SetUserStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
