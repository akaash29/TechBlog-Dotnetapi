using MediatR;
using UserRegistration.Application.DTOs.Roles;

namespace UserRegistration.Application.Features.Roles.GetAllRoles;

public sealed class GetAllRolesQuery : IRequest<IReadOnlyList<RoleDto>>
{
}
