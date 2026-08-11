using MediatR;
using UserRegistration.Application.DTOs.Auth;

namespace UserRegistration.Application.Features.Auth.RefreshToken;

public sealed class RefreshTokenCommand : IRequest<AuthResponse>
{
    public string RefreshToken { get; set; } = string.Empty;
}
