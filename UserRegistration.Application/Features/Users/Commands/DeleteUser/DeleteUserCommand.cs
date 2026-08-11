using MediatR;

namespace UserRegistration.Application.Features.Users.Commands.DeleteUser;

public sealed class DeleteUserCommand : IRequest
{
    public DeleteUserCommand(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; set; }
}
