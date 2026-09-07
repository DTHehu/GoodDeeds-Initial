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

    public async Task<EventDto?> GetEventById(Guid eventId)
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
    /// <summary>Returns null if the user does not belong to an organization.</summary>
    public async Task<EventDto?> CreateEvent(EventDto eventDto, Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.OrganizationId == null)
        {
            return null;
        }

        var newEvent = new Event()
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            Description = eventDto.Description,
            // Postgres timestamptz only accepts a UTC offset.
            EndTime = eventDto.EndTime.ToUniversalTime(),
            Location = eventDto.Location,
            OrganizationId = user.OrganizationId.Value,
            StartTime = eventDto.StartTime.ToUniversalTime(),
            Title = eventDto.Title
        };
        
        await _db.Events.AddAsync(newEvent);
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

    /// <summary>False means the event does not exist, or the user is already registered for it.</summary>
    public async Task<bool> RegisterForEvent(Guid userId, EventRegistrationRequest request)
    {
        if (!await _db.Events.AnyAsync(e => e.Id == request.EventId))
        {
            return false;
        }

        var alreadyRegistered = await _db.EventRegistrations
            .AnyAsync(r => r.EventId == request.EventId && r.UserId == userId);

        if (alreadyRegistered)
        {
            return false;
        }

        var registrationEntity = new EventRegistration()
        {
            EventId = request.EventId,
            UserId = userId,
            RegisteredAt = DateTimeOffset.UtcNow
        };

        await _db.EventRegistrations.AddAsync(registrationEntity);
        await _db.SaveChangesAsync();

        return true;
    }

    /// <summary>Returns null if the event does not exist, or the caller's organization does not own it.</summary>
    public async Task<List<EventRegistrationDto>?> GetEventRegistrations(Guid eventId, Guid callerId)
    {
        var eventEntity = await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId);
        if (eventEntity == null)
        {
            return null;
        }

        var caller = await _db.Users.FirstOrDefaultAsync(u => u.Id == callerId);
        if (caller?.OrganizationId != eventEntity.OrganizationId)
        {
            return null;
        }

        return await _db.EventRegistrations
            .Where(r => r.EventId == eventId)
            .Select(r => new EventRegistrationDto
            {
                UserId = r.UserId,
                Name = r.User.Name,
                Email = r.User.Email,
                Status = r.Status,
                RegisteredAt = r.RegisteredAt
            })
            .ToListAsync();
    }
}