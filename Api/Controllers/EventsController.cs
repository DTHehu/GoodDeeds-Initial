using System.Security.Claims;
using GoodDeedsApi.Models.Dtos;
using GoodDeedsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoodDeedsApi.Controllers;

[ApiController]
[Route("api/events")]
[Authorize(Policy = Policies.AuthenticatedUser)]
public class EventsController : ControllerBase
{
    private readonly EventService _events;
    
    public EventsController(EventService events)
    {
        _events = events;
    }
    
    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id) ? id : null;

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents()
    {
        var eventDtos = await _events.GetAllEvents();
        
        return Ok(eventDtos);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEventById(Guid id)
    {
        var eventDto = await _events.GetEventById(id);
        if (eventDto == null)
        {
            return NotFound();
        }

        return Ok(eventDto);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] EventDto eventDto)
    {
        if (CurrentUserId == null)
        {
            return Unauthorized();
        }

        if (eventDto.EndTime <= eventDto.StartTime)
        {
            return BadRequest("End time must be after start time.");
        }

        var createdEvent = await _events.CreateEvent(eventDto, CurrentUserId.Value);

        if (createdEvent == null)
        {
            return Forbid();
        }

        return Ok(createdEvent);
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterEvent([FromBody] EventRegistrationRequest eventRegistrationRequest)
    {
        if (CurrentUserId == null)
        {
            return Unauthorized();
        }

        var isRegistered = await _events.RegisterForEvent(CurrentUserId.Value, eventRegistrationRequest);

        if (!isRegistered)
        {
            return BadRequest("That event does not exist, organizations cannot register, or you are already registered for it.");
        }

        return Ok();
    }

    [HttpGet("registered")]
    public async Task<IActionResult> GetRegisteredEvents()
    {
        if (CurrentUserId == null)
        {
            return Unauthorized();
        }

        return Ok(await _events.GetRegisteredEvents(CurrentUserId.Value));
    }

    [HttpGet("{id}/registration")]
    public async Task<IActionResult> GetRegistrationStatus(Guid id)
    {
        if (CurrentUserId == null)
        {
            return Unauthorized();
        }

        return Ok(await _events.IsRegistered(CurrentUserId.Value, id));
    }

    [HttpDelete("{id}/register")]
    public async Task<IActionResult> UnregisterEvent(Guid id)
    {
        if (CurrentUserId == null)
        {
            return Unauthorized();
        }

        var unregistered = await _events.UnregisterForEvent(CurrentUserId.Value, id);

        return unregistered ? Ok() : NotFound();
    }

    /// <summary>Organizers only: the attendee list for one of their own events.</summary>
    [HttpGet("{id}/registrations")]
    public async Task<IActionResult> GetEventRegistrations(Guid id)
    {
        if (CurrentUserId == null)
        {
            return Unauthorized();
        }

        var registrations = await _events.GetEventRegistrations(id, CurrentUserId.Value);

        if (registrations == null)
        {
            return NotFound();
        }

        return Ok(registrations);
    }
}