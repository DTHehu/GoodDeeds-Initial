using GoodDeedsApi.Models.Dtos;
using GoodDeedsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoodDeedsApi.Controllers;

/// <summary>
/// Endpoints for reading and editing user profiles.
///
/// There is no "create user" here on purpose — signing up is handled by
/// Identity at POST /api/auth/register.
///
/// A controller's whole job is HTTP: read the request, call a service, turn
/// the answer into a status code. The real work lives in UserService.
/// Use this file as the template for the controllers you write next.
/// </summary>
[Route("api/users")]
[Authorize(Policy = Policies.AuthenticatedUser)]   // everything below needs a login
public class UsersController : ApiControllerBase
{
    private readonly UserService _users;

    // Same dependency injection idea as in UserService: we ask for what we
    // need, and ASP.NET Core supplies it. Registered in Program.cs.
    public UsersController(UserService users)
    {
        _users = users;
    }

    /// <summary>GET /api/users — every user. Administrators only.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        List<UserDto> users = await _users.GetAllAsync();
        return Ok(users);
    }

    /// <summary>GET /api/users/me — the signed-in user's own profile.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        if (CurrentUserId == null)
        {
            return Unauthorized();
        }

        UserDto? user = await _users.GetByIdAsync(CurrentUserId.Value);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>PUT /api/users/me — update your own name and phone number.</summary>
    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateUserRequest request)
    {
        if (CurrentUserId == null)
        {
            return Unauthorized();
        }

        UserDto? updated = await _users.UpdateAsync(CurrentUserId.Value, request);

        if (updated == null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    /// <summary>GET /api/users/{id} — one user. Yourself, or anyone if admin.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        // Without this check, any signed-in user could read every account.
        if (!CanActOnBehalfOf(id))
        {
            return Forbid();
        }

        UserDto? user = await _users.GetByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>PUT /api/users/{id} — edit a user. Yourself, or anyone if admin.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        if (!CanActOnBehalfOf(id))
        {
            return Forbid();
        }

        UserDto? updated = await _users.UpdateAsync(id, request);

        if (updated == null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    /// <summary>DELETE /api/users/{id} — administrators only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid id)
    {
        bool deleted = await _users.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        // 204 No Content: it worked, and there is nothing to send back.
        return NoContent();
    }
}
