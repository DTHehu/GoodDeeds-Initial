using System.Security.Claims;
using GoodDeedsApi.Data;
using GoodDeedsApi.Models;
using GoodDeedsApi.Models.Dtos;
using GoodDeedsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoodDeedsApi.Controllers;

/// <summary>
/// Account creation lives in Identity at POST /api/auth/register.
///
/// The class-level policy must be the loosest rule any action needs: [Authorize]
/// attributes combine with AND, so an action cannot widen what the class sets.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = Policies.AuthenticatedUser)]
public class UsersController : ControllerBase {
    private readonly UserService _users;
    private readonly OrganizationService _organizations;


    public UsersController(UserService users, OrganizationService organizations)
    {
        _users = users;
        _organizations = organizations;
    }

    /// <summary>
    /// Read from the token, so it cannot be forged. Prefer this over any user id
    /// taken from a request body.
    /// </summary>
    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id) ? id : null;

    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateUserRequest request) {
        if (CurrentUserId == null) {
            return Unauthorized();
        }

        var updated = await _users.UpdateAsync(CurrentUserId.Value, request);

        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrganizationById(Guid id) {
        var organizationDto = await _organizations.GetByIdAsync(id);

        if (organizationDto == null) {
            return NotFound();
        }

        return Ok(organizationDto);
    }
}
