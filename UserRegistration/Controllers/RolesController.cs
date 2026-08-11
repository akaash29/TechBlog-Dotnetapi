using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserRegistration.Application.DTOs.Roles;
using UserRegistration.Application.Features.Roles.GetAllRoles;

namespace UserRegistration.Controllers;

/// <summary>Read-only access to the Roles master table (Writer, Editor, Photographer, Reader, Admin).</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[AllowAnonymous]
public sealed class RolesController : ControllerBase
{
    private readonly ISender _sender;

    public RolesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Gets all available roles, e.g. to populate a registration form's role picker.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAllRolesQuery(), cancellationToken);
        return Ok(result);
    }
}
