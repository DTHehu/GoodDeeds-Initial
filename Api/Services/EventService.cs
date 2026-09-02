using GoodDeedsApi.Data;
using GoodDeedsApi.Models;
using GoodDeedsApi.Models.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GoodDeedsApi.Services;

public class EventService
{
    private readonly AppDbContext _db;
    private readonly RedisCacheService _cache;
    
    public EventService(AppDbContext db, RedisCacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<List<EventDto>> GetAllEvents()
    {
        var events = await _db.Events.ToListAsync();

        var eventsDtos = new List<EventDto>();
        foreach (var eventEntity in events)
        {
            eventsDtos.Add(new EventDto()
            {
                Id = eventEntity.Id,
                CreatedAt =  eventEntity.CreatedAt,
                Description =  eventEntity.Description,
                EndTime =  eventEntity.EndTime,
                Location =   eventEntity.Location,
                OrganizationId =  eventEntity.OrganizationId,
                StartTime =  eventEntity.StartTime,
                Title = eventEntity.Title
            });
        }

        return eventsDtos;
    }

    public async Task<EventDto> GetEventById(Guid eventId)
    {
        var eventEntity = await _db.Events.FindAsync(eventId);
        if (eventEntity == null)
        {
            return null;
        }

        return new EventDto()
        {
            Id = eventEntity.Id,
            CreatedAt = eventEntity.CreatedAt,
            Description = eventEntity.Description,
            EndTime = eventEntity.EndTime,
            Location = eventEntity.Location,
            OrganizationId = eventEntity.OrganizationId,
            StartTime = eventEntity.StartTime,
            Title = eventEntity.Title
        };
    }
    public async Task<EventDto> CreateEvent(EventDto eventDto, Guid userId)
    {
        var newEvent = new Event()
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Description = eventDto.Description,
            EndTime = eventDto.EndTime,
            Location = eventDto.Location,
            OrganizationId = userId,
            StartTime = eventDto.StartTime,
            Title = eventDto.Title
        };

        _db.Events.Add(newEvent);
        await _db.SaveChangesAsync();

        return new EventDto()
        {
            Id = newEvent.Id,
            CreatedAt = newEvent.CreatedAt,
            Description = newEvent.Description,
            EndTime = newEvent.EndTime,
            Location = newEvent.Location,
            OrganizationId = newEvent.OrganizationId,
            StartTime = newEvent.StartTime,
            Title = newEvent.Title
        };
    }
}