using MediatR;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;

namespace UserRegistration.Application.Features.Images.DiscardOrphanedImages;

public sealed class DiscardOrphanedImagesCommandHandler : IRequestHandler<DiscardOrphanedImagesCommand>
{
    private readonly IUploadedImageRepository _uploadedImageRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ICurrentUserService _currentUserService;

    public DiscardOrphanedImagesCommandHandler(
        IUploadedImageRepository uploadedImageRepository,
        IBlobStorageService blobStorageService,
        ICurrentUserService currentUserService)
    {
        _uploadedImageRepository = uploadedImageRepository;
        _blobStorageService = blobStorageService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DiscardOrphanedImagesCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to discard uploaded images.");

        var images = await _uploadedImageRepository.GetOrphanedByUserAsync(userId, cancellationToken);
        if (images.Count == 0)
        {
            return;
        }

        foreach (var image in images)
        {
            await _blobStorageService.DeleteAsync(image.ImagePath, cancellationToken);
        }

        _uploadedImageRepository.RemoveRange(images);
        await _uploadedImageRepository.SaveChangesAsync(cancellationToken);
    }
}
