using MediatR;
using UserRegistration.Application.DTOs.Users;

namespace UserRegistration.Application.Features.Users.Commands.UploadProfileImage;

public sealed class UploadProfileImageCommand : IRequest<UserDto>
{
    public required Guid UserId { get; init; }

    public required Stream Content { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public required long Length { get; init; }
}
