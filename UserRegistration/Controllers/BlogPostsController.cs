using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using UserRegistration.Application.Common.Models;
using UserRegistration.Application.DTOs.BlogPosts;
using UserRegistration.Application.Features.BlogPosts.Commands.ApproveBlogPost;
using UserRegistration.Application.Features.BlogPosts.Commands.CreateBlogPost;
using UserRegistration.Application.Features.BlogPosts.Commands.IncrementBlogPostView;
using UserRegistration.Application.Features.BlogPosts.Commands.LikeBlogPost;
using UserRegistration.Application.Features.BlogPosts.Commands.RejectBlogPost;
using UserRegistration.Application.Features.BlogPosts.Queries.GetAuthorStats;
using UserRegistration.Application.Features.BlogPosts.Queries.GetBlogPostById;
using UserRegistration.Application.Features.BlogPosts.Queries.GetFeedPosts;
using UserRegistration.Application.Features.BlogPosts.Queries.GetJournalPosts;
using UserRegistration.Application.Features.BlogPosts.Queries.GetLikedPostIds;
using UserRegistration.Application.Features.BlogPosts.Queries.GetPendingApprovalPosts;
using UserRegistration.Application.Features.BlogPosts.Queries.GetSuggestedBlogPosts;
using UserRegistration.Application.Features.BlogPosts.Queries.GetTopBlogPosts;
using UserRegistration.Domain.Enums;

namespace UserRegistration.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class BlogPostsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public BlogPostsController(ISender sender, IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    /// <summary>Saves a post. IsDraft on the request controls whether this is "Save
    /// draft" (relaxed validation, IsDraft = true) or "Publish" (IsDraft = false).
    /// A non-admin's "Publish" lands in PendingApproval rather than going live
    /// straight away — see CreateBlogPostCommandHandler.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BlogPostDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BlogPostDto>> Create(
        [FromBody] CreateBlogPostRequest request,
        CancellationToken cancellationToken)
    {
        var command = _mapper.Map<CreateBlogPostCommand>(request);
        var result = await _sender.Send(command, cancellationToken);
        // No GetById endpoint yet to point a Location header at — just the 201 + body.
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>The feed page's tab-ordered, paged post list.</summary>
    [HttpGet("feed")]
    [OutputCache(PolicyName = "BlogPostsPersonalized")]
    [ProducesResponseType(typeof(PagedResult<BlogPostSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<BlogPostSummaryDto>>> GetFeed(
        [FromQuery] string tab = "foryou",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetFeedPostsQuery(tab, page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>The journal page's newest-first, optionally category-filtered, paged post list.</summary>
    [HttpGet("journal")]
    [OutputCache(PolicyName = "BlogPostsPublic")]
    [ProducesResponseType(typeof(PagedResult<BlogPostSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<BlogPostSummaryDto>>> GetJournal(
        [FromQuery] int? categoryId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetJournalPostsQuery(categoryId, page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>Top posts by a single metric — powers the "most viewed"/"most
    /// liked" feed rails and the post page's "most read"/"most discussed" rail.</summary>
    [HttpGet("top")]
    [OutputCache(PolicyName = "BlogPostsPublic")]
    [ProducesResponseType(typeof(IReadOnlyList<BlogPostSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<BlogPostSummaryDto>>> GetTop(
        [FromQuery] string metric = "views",
        [FromQuery] int take = 4,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetTopBlogPostsQuery(metric, take), cancellationToken);
        return Ok(result);
    }

    /// <summary>A single post's full detail, for the post page.</summary>
    [HttpGet("{id:int}")]
    // Personalized, not just Public — a draft is only visible to its own
    // author, and a response cached without varying by who's asking could
    // leak a draft to someone else or hide it from its own author.
    [OutputCache(PolicyName = "BlogPostsPersonalized")]
    [ProducesResponseType(typeof(BlogPostDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlogPostDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetBlogPostByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Records a read — called once when the post page loads. Anonymous
    /// readers count too, same as the rest of the app's page-view analytics.</summary>
    [HttpPost("{id:int}/view")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordView(int id, CancellationToken cancellationToken)
    {
        await _sender.Send(new IncrementBlogPostViewCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Toggles the caller's like on a post — a second call un-likes it.</summary>
    [HttpPost("{id:int}/like")]
    [ProducesResponseType(typeof(LikeToggleResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LikeToggleResultDto>> Like(int id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new LikeBlogPostCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Every post the signed-in caller has liked — cross-referenced
    /// client-side to restore the like button's state after a reload.
    /// Empty for anonymous callers, not a 401.</summary>
    [HttpGet("liked")]
    [ProducesResponseType(typeof(IReadOnlyList<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<int>>> GetLiked(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLikedPostIdsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>"Suggested for you" — strong pieces from outside the caller's
    /// own beat, for the feed page's sidebar.</summary>
    [HttpGet("suggested")]
    [OutputCache(PolicyName = "BlogPostsPersonalized")]
    [ProducesResponseType(typeof(IReadOnlyList<BlogPostSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<BlogPostSummaryDto>>> GetSuggested(
        [FromQuery] int take = 3,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetSuggestedBlogPostsQuery(take), cancellationToken);
        return Ok(result);
    }

    /// <summary>Every post awaiting review, oldest first — the PendingApproval page.</summary>
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IReadOnlyList<BlogPostSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BlogPostSummaryDto>>> GetPending(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPendingApprovalPostsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Approves a pending post — publishes it.</summary>
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(typeof(BlogPostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BlogPostDto>> Approve(int id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ApproveBlogPostCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Rejects a pending post — deletes it and its uploaded images
    /// (blob storage included).</summary>
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(int id, CancellationToken cancellationToken)
    {
        await _sender.Send(new RejectBlogPostCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>The signed-in caller's own post/reads/comments/likes totals
    /// plus a 7-day reads breakdown, for the profile page's stat strip.</summary>
    [Authorize]
    [HttpGet("my-stats")]
    [ProducesResponseType(typeof(AuthorStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthorStatsDto>> GetMyStats(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAuthorStatsQuery(), cancellationToken);
        return Ok(result);
    }
}
