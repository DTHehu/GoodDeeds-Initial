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
[Route("api/users")]
[Authorize(Policy = Policies.AuthenticatedUser)]
public class UsersController : ApiControllerBase
{
    private readonly UserService _users;

    public UsersController(UserService users)
    {
        _users = users;
    }

    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        return Ok(await _users.GetAllAsync());
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        if (CurrentUserId == null)
        {
            return Unauthorized();
        }

        UserDto? user = await _users.GetByIdAsync(CurrentUserId.Value);

        return user == null ? NotFound() : Ok(user);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateUserRequest request)
    {
        if (CurrentUserId == null)
        {
            return Unauthorized();
        }

        UserDto? updated = await _users.UpdateAsync(CurrentUserId.Value, request);

        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        if (!CanActOnBehalfOf(id))
        {
            return Forbid();
        }

        UserDto? user = await _users.GetByIdAsync(id);

        return user == null ? NotFound() : Ok(user);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        if (!CanActOnBehalfOf(id))
        {
            return Forbid();
        }

        UserDto? updated = await _users.UpdateAsync(id, request);

        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid id)
    {
        return await _users.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
