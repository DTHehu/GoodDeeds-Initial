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
    
    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] EventDto eventDto)
    {
        if (CurrentUserId == null)
        {
            return Unauthorized();
        }

        var createdEvent = await _events.CreateEvent(eventDto, CurrentUserId.Value);
        
        return CreatedAtAction(nameof(GetEvents), new { id = createdEvent.Id }, createdEvent);
    }
}