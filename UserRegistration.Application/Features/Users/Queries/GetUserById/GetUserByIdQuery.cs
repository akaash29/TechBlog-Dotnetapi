using MediatR;
using UserRegistration.Application.DTOs.Users;

namespace UserRegistration.Application.Features.Users.Queries.GetUserById;

public sealed class GetUserByIdQuery : IRequest<UserDto>
{
    public GetUserByIdQuery(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; set; }
}
