using MediatR;

namespace UserRegistration.Application.Features.Images.DiscardOrphanedImages;

/// <summary>Deletes every image the caller has uploaded that never made it
/// into a saved post — both the blob storage files and their UploadedImage
/// rows. Called when the compose page's Clear button is confirmed, or when
/// the writer navigates away from an unsaved draft and confirms discarding
/// it (see CanDeactivate guard on the compose route).</summary>
public sealed record DiscardOrphanedImagesCommand : IRequest;
