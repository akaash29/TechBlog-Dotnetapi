using MediatR;
using UserRegistration.Application.DTOs.Users;

namespace UserRegistration.Application.Features.Users.Commands.SetUserStatus;

public sealed class SetUserStatusCommand : IRequest<UserDto>
{
    public Guid Id { get; set; }

    public bool IsActive { get; set; }
}
