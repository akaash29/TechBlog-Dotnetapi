using MediatR;
using UserRegistration.Application.DTOs.Users;

namespace UserRegistration.Application.Features.Users.Queries.GetUserByEmail;

public sealed class GetUserByEmailQuery : IRequest<UserDto>
{
    public GetUserByEmailQuery(string email)
    {
        Email = email;
    }

    public string Email { get; set; } = string.Empty;
}
