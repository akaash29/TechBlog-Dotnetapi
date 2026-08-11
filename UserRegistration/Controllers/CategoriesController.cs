using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using UserRegistration.Application.DTOs.Categories;
using UserRegistration.Application.Features.Categories.GetAllCategories;

namespace UserRegistration.Controllers;

/// <summary>Read-only access to the Categories master table, e.g. to populate the compose page's category picker.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ISender _sender;

    public CategoriesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [OutputCache(PolicyName = "Categories")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAllCategoriesQuery(), cancellationToken);
        return Ok(result);
    }
}
