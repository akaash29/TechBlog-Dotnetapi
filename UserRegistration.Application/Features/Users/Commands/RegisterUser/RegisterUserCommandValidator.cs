using FluentValidation;
using UserRegistration.Domain.Enums;

namespace UserRegistration.Application.Features.Users.Commands.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    private static readonly string[] SelfRegisterableRoles = Enum.GetNames<UserRole>()
        .Where(name => name != nameof(UserRole.Admin))
        .ToArray();

    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches("^[a-zA-Z0-9_.-]+$")
            .WithMessage("Username may only contain letters, digits, dots, underscores and hyphens.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        // Admin is intentionally excluded: self-service sign-up must never grant elevated
        // privileges. Admin accounts are provisioned via the authenticated Create endpoint.
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => SelfRegisterableRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Role must be one of: {string.Join(", ", SelfRegisterableRoles)}.");

        // Complexity (upper/lower/digit) is intentionally not enforced — strength
        // is surfaced to the user as a hint (see the Angular meter), not a gate.
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);
    }
}
