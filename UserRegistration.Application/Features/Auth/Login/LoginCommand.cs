using MediatR;
using UserRegistration.Application.DTOs.Auth;

namespace UserRegistration.Application.Features.Auth.Login;

public sealed class LoginCommand : IRequest<AuthResponse>
{
    public string EmailOrUserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
