using GoodDeedsApi.Data;
using GoodDeedsApi.Models;
using GoodDeedsApi.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GoodDeedsApi.Services;

public class EventService(AppDbContext db, RedisCacheService cache)
{
    private static string CacheKey(Guid id) => $"event:{id}";

    public async Task<IReadOnlyList<EventDto>> GetAllAsync(
        Guid? organizationId = null, bool upcomingOnly = false, CancellationToken ct = default)
    {
        var query = db.Events.AsNoTracking();

        if (organizationId is { } orgId)
            query = query.Where(e => e.OrganizationId == orgId);

        if (upcomingOnly)
        {
            var now = DateTimeOffset.UtcNow;
            query = query.Where(e => e.EndTime >= now);
        }

        return await query
            .OrderBy(e => e.StartTime)
            .Select(e => new EventDto(
                e.Id, e.OrganizationId, e.Title, e.Description, e.Location,
                e.StartTime, e.EndTime, e.CreatedAt,
                e.Registrations.Count(r => r.Status == RegistrationStatus.Registered)))
            .ToListAsync(ct);
    }

    public async Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cached = await cache.GetAsync<EventDto>(CacheKey(id), ct);
        if (cached is not null) return cached;

        var ev = await db.Events
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EventDto(
                e.Id, e.OrganizationId, e.Title, e.Description, e.Location,
                e.StartTime, e.EndTime, e.CreatedAt,
                e.Registrations.Count(r => r.Status == RegistrationStatus.Registered)))
            .FirstOrDefaultAsync(ct);

        if (ev is not null)
            await cache.SetAsync(CacheKey(id), ev, ct: ct);

        return ev;
    }

    public async Task<ServiceResult<EventDto>> CreateAsync(CreateEventRequest request, CancellationToken ct = default)
    {
        if (request.EndTime <= request.StartTime)
            return ServiceResult<EventDto>.Invalid("EndTime must be strictly greater than StartTime.");

        if (!await db.Organizations.AnyAsync(o => o.Id == request.OrganizationId, ct))
            return ServiceResult<EventDto>.NotFound($"Organization '{request.OrganizationId}' was not found.");

        var ev = new Event
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            Title = request.Title.Trim(),
            Description = request.Description,
            Location = request.Location?.Trim(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Events.Add(ev);
        await db.SaveChangesAsync(ct);

        return ServiceResult<EventDto>.Ok(ToDto(ev, registeredCount: 0));
    }

    public async Task<ServiceResult<EventDto>> UpdateAsync(
        Guid id, UpdateEventRequest request, CancellationToken ct = default)
    {
        if (request.EndTime <= request.StartTime)
            return ServiceResult<EventDto>.Invalid("EndTime must be strictly greater than StartTime.");

        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (ev is null)
            return ServiceResult<EventDto>.NotFound($"Event '{id}' was not found.");

        ev.Title = request.Title.Trim();
        ev.Description = request.Description;
        ev.Location = request.Location?.Trim();
        ev.StartTime = request.StartTime;
        ev.EndTime = request.EndTime;

        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKey(id), ct);

        var registeredCount = await db.EventRegistrations
            .CountAsync(r => r.EventId == id && r.Status == RegistrationStatus.Registered, ct);

        return ServiceResult<EventDto>.Ok(ToDto(ev, registeredCount));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await db.Events.Where(e => e.Id == id).ExecuteDeleteAsync(ct);
        if (deleted > 0) await cache.RemoveAsync(CacheKey(id), ct);
        return deleted > 0;
    }

    private static EventDto ToDto(Event e, int registeredCount) =>
        new(e.Id, e.OrganizationId, e.Title, e.Description, e.Location,
            e.StartTime, e.EndTime, e.CreatedAt, registeredCount);
}
