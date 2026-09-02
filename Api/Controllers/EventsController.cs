using System.Security.Claims;
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

    [HttpGet("events/{id}")]
    public async Task<IActionResult> GetEvent([FromRoute] Guid id)
    {
        var eventDto = await _events.GetEventById(id);

        if (eventDto == null)
            return NotFound();

        return Ok(eventDto);
    }
}