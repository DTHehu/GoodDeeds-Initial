using GoodDeedsApi.Models.Dtos;
using GoodDeedsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoodDeedsApi.Controllers;

/// <summary>
/// Browsing events is public. Creating and editing them is admin-only.
/// Registering is for any signed-in user, acting on their own behalf.
///
/// The controller-level attribute is the *loosest* rule that applies to any
/// action here, because [Authorize] attributes stack: a controller policy and
/// an action policy must BOTH pass. Putting AdminOnly at the controller level
/// would make it impossible for an action to widen access back out to ordinary
/// users, so the admin policy is applied per action instead.
/// </summary>
[Route("api/events")]
[Authorize(Policy = Policies.AuthenticatedUser)]
public class EventsController(
    EventService events,
    EventRegistrationService registrations) : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<EventDto>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] bool upcomingOnly,
        CancellationToken ct) =>
        Ok(await events.GetAllAsync(organizationId, upcomingOnly, ct));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<EventDto>> GetById(Guid id, CancellationToken ct)
    {
        var ev = await events.GetByIdAsync(id, ct);
        return ev is null ? NotFound() : Ok(ev);
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<EventDto>> Create([FromBody] CreateEventRequest request, CancellationToken ct)
    {
        var result = await events.CreateAsync(request, ct);
        if (!result.Succeeded) return Failure(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<EventDto>> Update(
        Guid id, [FromBody] UpdateEventRequest request, CancellationToken ct)
    {
        var result = await events.UpdateAsync(id, request, ct);
        return result.Succeeded ? Ok(result.Value) : Failure(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await events.DeleteAsync(id, ct) ? NoContent() : NotFound();

    // ---------- Registrations ----------

    /// <summary>The attendee list. Organizers only.</summary>
    [HttpGet("{id:guid}/registrations")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<IReadOnlyList<EventRegistrationDto>>> GetRegistrations(
        Guid id, CancellationToken ct)
    {
        var result = await registrations.GetForEventAsync(id, ct);
        return result.Succeeded ? Ok(result.Value) : Failure(result);
    }

    /// <summary>
    /// Signs the caller up. The body may name a different user, but only an
    /// admin is allowed to do that, so an ordinary account cannot register
    /// anyone but itself.
    /// </summary>
    [HttpPost("{id:guid}/registrations")]
    public async Task<ActionResult<EventRegistrationDto>> Register(
        Guid id, [FromBody] RegisterForEventRequest? request, CancellationToken ct)
    {
        if (CurrentUserId is not { } callerId) return Unauthorized();

        var targetUserId = request?.UserId ?? callerId;
        if (!CanActOnBehalfOf(targetUserId)) return Forbid();

        var result = await registrations.RegisterAsync(id, targetUserId, ct);
        if (!result.Succeeded) return Failure(result);

        return CreatedAtAction(nameof(GetRegistrations), new { id }, result.Value);
    }

    /// <summary>Marking someone attended or waitlisted is an organizer action.</summary>
    [HttpPut("{id:guid}/registrations/{userId:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<EventRegistrationDto>> UpdateRegistrationStatus(
        Guid id, Guid userId, [FromBody] UpdateRegistrationStatusRequest request, CancellationToken ct)
    {
        var result = await registrations.UpdateStatusAsync(id, userId, request.Status, ct);
        return result.Succeeded ? Ok(result.Value) : Failure(result);
    }

    /// <summary>
    /// Soft cancel. The row is kept so the signup is still auditable.
    /// Users can cancel their own registration; admins can cancel anyone's.
    /// </summary>
    [HttpDelete("{id:guid}/registrations/{userId:guid}")]
    public async Task<IActionResult> CancelRegistration(Guid id, Guid userId, CancellationToken ct)
    {
        if (!CanActOnBehalfOf(userId)) return Forbid();

        return await registrations.CancelAsync(id, userId, ct) ? NoContent() : NotFound();
    }
}
