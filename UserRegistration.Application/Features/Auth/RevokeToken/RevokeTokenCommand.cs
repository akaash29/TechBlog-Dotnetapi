using MediatR;

namespace UserRegistration.Application.Features.Auth.RevokeToken;

public sealed class RevokeTokenCommand : IRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
