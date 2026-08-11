using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserRegistration.Application.Common.Constants;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.DTOs.Messages;
using UserRegistration.Application.Features.Messages.Commands.MarkThreadRead;
using UserRegistration.Application.Features.Messages.Commands.SendMessage;
using UserRegistration.Application.Features.Messages.Commands.UploadMessageAttachment;
using UserRegistration.Application.Features.Messages.Queries.GetConversations;
using UserRegistration.Application.Features.Messages.Queries.GetMessageThread;
using UserRegistration.Application.Features.Messages.Queries.GetUnreadMessageCount;

namespace UserRegistration.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public sealed class MessagesController : ControllerBase
{
    private readonly ISender _sender;

    public MessagesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>The caller's conversations, newest first — one row per other participant.</summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(IReadOnlyList<ConversationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConversationDto>>> GetConversations(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetConversationsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>The most recent messages between the caller and another user, oldest first.</summary>
    [HttpGet("thread/{otherUserId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<MessageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MessageDto>>> GetThread(
        Guid otherUserId,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetMessageThreadQuery(otherUserId, take), cancellationToken);
        return Ok(result);
    }

    /// <summary>Sends a message. Needs text, an attachment, a voice note, or some
    /// combination — see SendMessageCommandValidator.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageDto>> Send(
        [FromBody] SendMessageCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Uploads a file attachment or voice note (max 5 MB) to blob
    /// storage ahead of sending the message it belongs to.</summary>
    [HttpPost("attachments")]
    [RequestSizeLimit(MessageAttachmentConstraints.MaxSizeBytes + 1024 * 1024)]
    [ProducesResponseType(typeof(UploadMessageAttachmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadMessageAttachmentResponse>> UploadAttachment(
        [FromForm] Guid recipientId,
        [FromForm] string kind,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new ValidationException([new ValidationFailure(nameof(file), "Choose a file to upload.")]);
        }

        await using var stream = file.OpenReadStream();

        var command = new UploadMessageAttachmentCommand
        {
            RecipientId = recipientId,
            Content = stream,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            Kind = kind,
        };

        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Marks every message from the other user as read.</summary>
    [HttpPost("thread/{otherUserId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkThreadRead(Guid otherUserId, CancellationToken cancellationToken)
    {
        await _sender.Send(new MarkThreadReadCommand(otherUserId), cancellationToken);
        return NoContent();
    }

    /// <summary>Total unread messages across every conversation — the sidebar badge.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUnreadMessageCountQuery(), cancellationToken);
        return Ok(result);
    }
}
