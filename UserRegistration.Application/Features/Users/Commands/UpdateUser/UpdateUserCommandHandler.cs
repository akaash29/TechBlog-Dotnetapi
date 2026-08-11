using AutoMapper;
using MediatR;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.Users;
using UserRegistration.Domain.Entities;
using UserRegistration.Domain.Enums;

namespace UserRegistration.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), request.Id);

        var callerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to update a profile.");
        var isAdmin = _currentUserService.IsInRole(nameof(UserRole.Admin));

        // Anyone can edit their own profile; only an admin can edit someone else's.
        if (!isAdmin && callerId != request.Id)
        {
            throw new ForbiddenException("You can only update your own profile.");
        }

        // Role/active-status changes stay admin-only even on a self-edit — a
        // self-service profile form has no business granting itself a new role,
        // and trusting whatever the client happened to send here would let a
        // tampered request body do exactly that. A non-admin's own request
        // still round-trips its own current role/status, so the handler applies
        // uniformly either way.
        var role = isAdmin ? Enum.Parse<UserRole>(request.Role, ignoreCase: true) : (UserRole)user.RoleId;
        var isActive = isAdmin ? request.IsActive : user.IsActive;

        user.UpdateProfile(request.FirstName, request.LastName, request.Phone, request.City, role, isActive);

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }
}
