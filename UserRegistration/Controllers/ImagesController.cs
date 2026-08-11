using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserRegistration.Application.Common.Constants;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.DTOs.Images;
using UserRegistration.Application.Features.Images.DiscardOrphanedImages;
using UserRegistration.Application.Features.Images.UploadImage;

namespace UserRegistration.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class ImagesController : ControllerBase
{
    private readonly ISender _sender;

    public ImagesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Uploads a cover or body image for the compose editor (max 10 MB) to blob
    /// storage and records it. Not yet linked to a post — see CreateBlogPost.</summary>
    [HttpPost("upload")]
    // A little headroom above the 10 MB content limit for multipart boundary/header overhead;
    // the actual file-size rule is enforced on File.Length by UploadImageCommandValidator.
    [RequestSizeLimit(ImageUploadConstraints.MaxSizeBytes + 1024 * 1024)]
    [ProducesResponseType(typeof(UploadImageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadImageResponse>> Upload(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new ValidationException([new ValidationFailure(nameof(file), "Choose an image to upload.")]);
        }

        await using var stream = file.OpenReadStream();

        var command = new UploadImageCommand
        {
            Content = stream,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length
        };

        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Deletes every image the caller uploaded that never made it
    /// into a saved post — blob storage files included. Called when the
    /// compose page's Clear button, or an unsaved-changes navigation guard,
    /// is confirmed.</summary>
    [Authorize]
    [HttpPost("discard-orphaned")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DiscardOrphaned(CancellationToken cancellationToken)
    {
        await _sender.Send(new DiscardOrphanedImagesCommand(), cancellationToken);
        return NoContent();
    }
}
