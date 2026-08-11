using AutoMapper;
using MediatR;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.Users;
using UserRegistration.Domain.Entities;
using UserRegistration.Domain.Enums;

namespace UserRegistration.Application.Features.Users.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new ConflictException($"A user with email '{request.Email}' is already registered.");
        }

        if (await _userRepository.ExistsByUserNameAsync(request.UserName, cancellationToken))
        {
            throw new ConflictException($"Username '{request.UserName}' is already taken.");
        }

        var role = Enum.Parse<UserRole>(request.Role, ignoreCase: true);

        var user = new User
        {
            UserName = request.UserName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            RoleId = (int)role,
            IsActive = true
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // user.Role is never loaded on this freshly-constructed entity (only
        // RoleId is set), so AutoMapper's UserDto.Role mapping (src.Role.Name)
        // would come back null. request.Role was already validated against
        // the enum names, so its canonical casing is the correct value.
        var dto = _mapper.Map<UserDto>(user);
        dto.Role = role.ToString();
        return dto;
    }
}
