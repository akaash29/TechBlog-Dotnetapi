using FluentValidation;

namespace UserRegistration.Application.Features.Auth.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.EmailOrUserName)
            .NotEmpty();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
