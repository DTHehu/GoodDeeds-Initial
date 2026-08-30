using GoodDeedsApi.Models.Dtos;
using GoodDeedsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoodDeedsApi.Controllers;

/// <summary>
/// Browsing organizations is public so the marketing pages and the event feed
/// work for signed-out visitors. Everything that writes is admin-only.
/// </summary>
[Route("api/organizations")]
[Authorize(Policy = Policies.AuthenticatedUser)]
public class OrganizationsController(OrganizationService organizations) : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<OrganizationDto>>> GetAll(CancellationToken ct) =>
        Ok(await organizations.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<OrganizationDto>> GetById(Guid id, CancellationToken ct)
    {
        var organization = await organizations.GetByIdAsync(id, ct);
        return organization is null ? NotFound() : Ok(organization);
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<OrganizationDto>> Create(
        [FromBody] CreateOrganizationRequest request, CancellationToken ct)
    {
        var result = await organizations.CreateAsync(request, ct);
        if (!result.Succeeded) return Failure(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<OrganizationDto>> Update(
        Guid id, [FromBody] UpdateOrganizationRequest request, CancellationToken ct)
    {
        var result = await organizations.UpdateAsync(id, request, ct);
        return result.Succeeded ? Ok(result.Value) : Failure(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await organizations.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
