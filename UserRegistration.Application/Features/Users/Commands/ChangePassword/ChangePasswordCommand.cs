using MediatR;

namespace UserRegistration.Application.Features.Users.Commands.ChangePassword;

public sealed class ChangePasswordCommand : IRequest
{
    public Guid Id { get; set; }

    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;

    public string ConfirmNewPassword { get; set; } = string.Empty;
}
