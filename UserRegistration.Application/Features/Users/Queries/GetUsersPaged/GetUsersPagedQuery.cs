using MediatR;
using UserRegistration.Application.Common.Models;
using UserRegistration.Application.DTOs.Users;
using UserRegistration.Domain.Enums;

namespace UserRegistration.Application.Features.Users.Queries.GetUsersPaged;

public sealed class GetUsersPagedQuery : IRequest<PagedResult<UserDto>>
{
    public GetUsersPagedQuery(int page, int pageSize, UserRole? role, bool? isActive, string? search = null)
    {
        Page = page;
        PageSize = pageSize;
        Role = role;
        IsActive = isActive;
        Search = search;
    }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public UserRole? Role { get; set; }

    public bool? IsActive { get; set; }

    public string? Search { get; set; }
}
