using MediatR;
using UserRegistration.Application.DTOs.Users;

namespace UserRegistration.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommand : IRequest<UserDto>
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? City { get; set; }

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
