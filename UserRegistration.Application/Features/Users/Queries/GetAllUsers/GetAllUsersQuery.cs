using MediatR;
using UserRegistration.Application.DTOs.Users;

namespace UserRegistration.Application.Features.Users.Queries.GetAllUsers;

public sealed class GetAllUsersQuery : IRequest<IReadOnlyList<UserDto>>
{
}
