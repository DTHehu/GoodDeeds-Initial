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

        return CreatedAtAction(nameof(GetEventById), new { id = createdEvent.Id }, createdEvent);
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterEvent([FromBody] EventRegestrationRequest eventRegestrationRequest) 
    {
        if (CurrentUserId == null) 
        {
            return Unauthorized();
        }

        var isRegistered = await _events.RegisterForEvent(CurrentUserId.Value, eventRegestrationRequest);

        if (!isRegistered) 
        {
            return BadRequest("Failed to Register Event.");
        }

        return Ok();
    }

    [HttpGet("{id}/registrations")]
    public async Task<IActionResult> GetEventRegistrations(Guid id) 
    {

        if (CurrentUserId == null) 
        {
            return Unauthorized();
        }

        var registrations = await _events.GetEventRegistrations(id);

        return Ok(registrations);
    }

}