using MediatR;
using UserRegistration.Application.DTOs.Images;

namespace UserRegistration.Application.Features.Images.UploadImage;

public sealed class UploadImageCommand : IRequest<UploadImageResponse>
{
    public required Stream Content { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public required long Length { get; init; }
}
