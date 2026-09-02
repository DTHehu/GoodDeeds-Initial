using GoodDeedsApi.Data;
using GoodDeedsApi.Models;
using GoodDeedsApi.Models.Dtos;
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
        var cacheKey = $"publicEvents";

        var cached = await _cache.GetAsync<List<EventDto>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }
        
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
        
        await _cache.SetAsync(cacheKey, eventsDtos);

        return eventsDtos;
    }

    public async Task<EventDto?> GetEventById(Guid id)
    {
        var eventEntity = await _db.Events.FindAsync(id);
        if (eventEntity == null)
            return null;

        var entityDto = new EventDto()
        {
            CreatedAt = eventEntity.CreatedAt,
            Description = eventEntity.Description,
            EndTime = eventEntity.EndTime,
            Id = eventEntity.Id,
            Location = eventEntity.Location,
            OrganizationId = eventEntity.OrganizationId,
            StartTime = eventEntity.StartTime,
            Title = eventEntity.Title
        };
        
        return entityDto;
    }
}