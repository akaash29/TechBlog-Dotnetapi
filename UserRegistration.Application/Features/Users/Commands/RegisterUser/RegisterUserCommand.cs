using MediatR;
using UserRegistration.Application.DTOs.Users;

namespace UserRegistration.Application.Features.Users.Commands.RegisterUser;

public sealed class RegisterUserCommand : IRequest<UserDto>
{
    public string UserName { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
