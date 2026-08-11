using MediatR;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;

namespace UserRegistration.Application.Features.Users.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IUserRepository _userRepository;

    public DeleteUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), request.Id);

        _userRepository.Remove(user);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }
}
