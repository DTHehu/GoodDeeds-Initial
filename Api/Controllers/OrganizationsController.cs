using GoodDeedsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoodDeedsApi.Controllers;

[ApiController]
[Route("api/organizations")]
[Authorize(Policy = Policies.AuthenticatedUser)]
public class OrganizationsController : ControllerBase
{
    private readonly OrganizationService _organizations;

    public OrganizationsController(OrganizationService organizations)
    {
        _organizations = organizations;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var organization = await _organizations.GetByIdAsync(id);

        return organization == null ? NotFound() : Ok(organization);
    }
}
