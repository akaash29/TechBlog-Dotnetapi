using AutoMapper;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserRegistration.Application.Common.Constants;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Models;
using UserRegistration.Application.DTOs.Users;
using UserRegistration.Application.Features.Users.Commands.ChangePassword;
using UserRegistration.Application.Features.Users.Commands.CreateUser;
using UserRegistration.Application.Features.Users.Commands.DeleteUser;
using UserRegistration.Application.Features.Users.Commands.RegisterUser;
using UserRegistration.Application.Features.Users.Commands.SetUserStatus;
using UserRegistration.Application.Features.Users.Commands.UpdateUser;
using UserRegistration.Application.Features.Users.Commands.UploadProfileImage;
using UserRegistration.Application.Features.Users.Queries.GetAllUsers;
using UserRegistration.Application.Features.Users.Queries.GetUserByEmail;
using UserRegistration.Application.Features.Users.Queries.GetUserById;
using UserRegistration.Application.Features.Users.Queries.GetUsersPaged;
using UserRegistration.Domain.Enums;

namespace UserRegistration.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public UsersController(ISender sender, IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    /// <summary>Registers a new user account (public self-service sign-up).</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = _mapper.Map<RegisterUserCommand>(request);
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Creates a user directly (administrative create).</summary>
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = _mapper.Map<CreateUserCommand>(request);
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Gets a single user by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUserByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Gets all users.</summary>
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAllUsersQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Gets a single user by email address.</summary>
    [HttpGet("by-email")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetByEmail([FromQuery] string email, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUserByEmailQuery(email), cancellationToken);
        return Ok(result);
    }

    /// <summary>Gets a paged, optionally filtered list of users.</summary>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetUsersPagedQuery(page, pageSize, role, isActive, search), cancellationToken);
        return Ok(result);
    }

    /// <summary>Updates an existing user's profile.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = _mapper.Map<UpdateUserCommand>(request);
        command.Id = id;
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Uploads (and replaces) a user's profile photo. Callers may only
    /// upload their own photo unless they're an admin.</summary>
    [HttpPost("{id:guid}/profile-image")]
    // A little headroom above the 10 MB content limit for multipart boundary/header overhead;
    // the actual file-size rule is enforced on file.Length by UploadProfileImageCommandValidator.
    [RequestSizeLimit(ImageUploadConstraints.MaxSizeBytes + 1024 * 1024)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> UploadProfileImage(
        Guid id,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new ValidationException([new ValidationFailure(nameof(file), "Choose an image to upload.")]);
        }

        await using var stream = file.OpenReadStream();

        var command = new UploadProfileImageCommand
        {
            UserId = id,
            Content = stream,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length
        };

        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Changes a user's password.</summary>
    [HttpPut("{id:guid}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        Guid id,
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var command = _mapper.Map<ChangePasswordCommand>(request);
        command.Id = id;
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Activates or deactivates a user.</summary>
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> SetStatus(
        Guid id,
        [FromBody] SetUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = _mapper.Map<SetUserStatusCommand>(request);
        command.Id = id;
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Deletes a user.</summary>
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteUserCommand(id), cancellationToken);
        return NoContent();
    }
}
