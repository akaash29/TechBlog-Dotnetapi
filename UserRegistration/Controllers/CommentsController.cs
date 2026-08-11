using MediatR;
using Microsoft.AspNetCore.Mvc;
using UserRegistration.Application.DTOs.Comments;
using UserRegistration.Application.Features.Comments.Commands.AddComment;
using UserRegistration.Application.Features.Comments.Commands.DeleteComment;
using UserRegistration.Application.Features.Comments.Commands.UpdateComment;
using UserRegistration.Application.Features.Comments.Queries.GetCommentsByBlogPostId;

namespace UserRegistration.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class CommentsController : ControllerBase
{
    private readonly ISender _sender;

    public CommentsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>All comments on a post, oldest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CommentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> GetByPost(
        [FromQuery] int blogPostId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCommentsByBlogPostIdQuery(blogPostId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Adds a comment and bumps the post's CommentsCount.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> Add(
        [FromBody] AddCommentCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Edits a comment's text. Only the comment's author or an admin may.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> Update(
        int id,
        [FromBody] UpdateCommentCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Deletes a comment and drops the post's CommentsCount. Only
    /// the comment's author or an admin may.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteCommentCommand(id), cancellationToken);
        return NoContent();
    }
}
