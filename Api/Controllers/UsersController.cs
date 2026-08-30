using GoodDeedsApi.Models.Dtos;
using GoodDeedsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoodDeedsApi.Controllers;

/// <summary>
/// Account creation is not here: it belongs to Identity at
/// POST /api/auth/register. This controller covers reading and maintaining
/// profiles that already exist.
///
/// The whole controller requires a signed-in caller. Individual actions
/// tighten that further to admin-only, or to "yourself or an admin".
/// </summary>
[Route("api/users")]
[Authorize(Policy = Policies.AuthenticatedUser)]
public class UsersController(
    IUserService users,
    IEventRegistrationService registrations) : ApiControllerBase
{
    /// <summary>Listing every account is an administrative view.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken ct) =>
        Ok(await users.GetAllAsync(ct));

    /// <summary>The caller's own profile. Convenience wrapper over GetById.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe(CancellationToken ct)
    {
        if (CurrentUserId is not { } id) return Unauthorized();

        var user = await users.GetByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe(
        [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } id) return Unauthorized();

        var result = await users.UpdateAsync(id, request, ct);
        return result.Succeeded ? Ok(result.Value) : Failure(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken ct)
    {
        // Without this check any signed-in user could enumerate every account.
        if (!CanActOnBehalfOf(id)) return Forbid();

        var user = await users.GetByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> Update(
        Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        if (!CanActOnBehalfOf(id)) return Forbid();

        var result = await users.UpdateAsync(id, request, ct);
        return result.Succeeded ? Ok(result.Value) : Failure(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await users.DeleteAsync(id, ct) ? NoContent() : NotFound();

    /// <summary>Every event this user is signed up for, excluding cancellations.</summary>
    [HttpGet("{id:guid}/events")]
    public async Task<ActionResult<IReadOnlyList<EventDto>>> GetEvents(Guid id, CancellationToken ct)
    {
        if (!CanActOnBehalfOf(id)) return Forbid();

        var result = await registrations.GetEventsForUserAsync(id, ct);
        return result.Succeeded ? Ok(result.Value) : Failure(result);
    }
}
