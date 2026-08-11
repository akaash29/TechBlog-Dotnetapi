using AutoMapper;
using MediatR;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.Common.Models;
using UserRegistration.Application.DTOs.Users;

namespace UserRegistration.Application.Features.Users.Queries.GetUsersPaged;

public sealed class GetUsersPagedQueryHandler : IRequestHandler<GetUsersPagedQuery, PagedResult<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUsersPagedQueryHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<UserDto>> Handle(GetUsersPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _userRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.Role,
            request.IsActive,
            request.Search,
            cancellationToken);

        var mappedItems = _mapper.Map<IReadOnlyList<UserDto>>(items);

        return new PagedResult<UserDto>(mappedItems, request.Page, request.PageSize, totalCount);
    }
}
