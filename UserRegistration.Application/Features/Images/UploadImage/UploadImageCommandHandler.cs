using MediatR;
using UserRegistration.Application.Common.Constants;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.Images;
using UserRegistration.Domain.Entities;

namespace UserRegistration.Application.Features.Images.UploadImage;

public sealed class UploadImageCommandHandler : IRequestHandler<UploadImageCommand, UploadImageResponse>
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly IUploadedImageRepository _uploadedImageRepository;
    private readonly ICurrentUserService _currentUserService;

    public UploadImageCommandHandler(
        IBlobStorageService blobStorageService,
        IUploadedImageRepository uploadedImageRepository,
        ICurrentUserService currentUserService)
    {
        _blobStorageService = blobStorageService;
        _uploadedImageRepository = uploadedImageRepository;
        _currentUserService = currentUserService;
    }

    public async Task<UploadImageResponse> Handle(UploadImageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to upload images.");

        // A random name keeps concurrent uploads of same-named files from colliding,
        // grouped under the "images/" virtual folder inside the "blogpost" container.
        var extension = Path.GetExtension(request.FileName);
        var blobName = $"{Guid.NewGuid():N}{extension}";
        var blobPath = $"{BlobContainers.BlogPostImagesFolder}/{blobName}";

        var imagePath = await _blobStorageService.UploadAsync(
            BlobContainers.BlogPost, blobPath, request.Content, request.ContentType, cancellationToken);

        var image = new UploadedImage
        {
            ImagePath = imagePath,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        };

        await _uploadedImageRepository.AddAsync(image, cancellationToken);
        await _uploadedImageRepository.SaveChangesAsync(cancellationToken);

        return new UploadImageResponse { Id = image.Id, ImagePath = image.ImagePath };
    }
}
