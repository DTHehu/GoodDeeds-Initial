using GoodDeedsApi.Data;
using GoodDeedsApi.Models;
using GoodDeedsApi.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GoodDeedsApi.Services;

public class EventRegistrationService(AppDbContext db, ICacheService cache) : IEventRegistrationService
{
    public async Task<ServiceResult<IReadOnlyList<EventRegistrationDto>>> GetForEventAsync(
        Guid eventId, CancellationToken ct = default)
    {
        if (!await db.Events.AnyAsync(e => e.Id == eventId, ct))
            return ServiceResult<IReadOnlyList<EventRegistrationDto>>.NotFound($"Event '{eventId}' was not found.");

        var registrations = await db.EventRegistrations
            .AsNoTracking()
            .Where(r => r.EventId == eventId)
            .OrderBy(r => r.RegisteredAt)
            .Select(r => new EventRegistrationDto(r.EventId, r.UserId, r.Status, r.RegisteredAt))
            .ToListAsync(ct);

        return ServiceResult<IReadOnlyList<EventRegistrationDto>>.Ok(registrations);
    }

    public async Task<ServiceResult<IReadOnlyList<EventDto>>> GetEventsForUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
            return ServiceResult<IReadOnlyList<EventDto>>.NotFound($"User '{userId}' was not found.");

        var events = await db.EventRegistrations
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.Status != RegistrationStatus.Cancelled)
            .OrderBy(r => r.Event.StartTime)
            .Select(r => new EventDto(
                r.Event.Id, r.Event.OrganizationId, r.Event.Title, r.Event.Description,
                r.Event.Location, r.Event.StartTime, r.Event.EndTime, r.Event.CreatedAt,
                r.Event.Registrations.Count(x => x.Status == RegistrationStatus.Registered)))
            .ToListAsync(ct);

        return ServiceResult<IReadOnlyList<EventDto>>.Ok(events);
    }

    public async Task<ServiceResult<EventRegistrationDto>> RegisterAsync(
        Guid eventId, Guid userId, CancellationToken ct = default)
    {
        if (!await db.Events.AnyAsync(e => e.Id == eventId, ct))
            return ServiceResult<EventRegistrationDto>.NotFound($"Event '{eventId}' was not found.");

        if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
            return ServiceResult<EventRegistrationDto>.NotFound($"User '{userId}' was not found.");

        var existing = await db.EventRegistrations
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId, ct);

        if (existing is not null)
        {
            // Re-registering after a cancellation reuses the row rather than
            // colliding with the composite primary key.
            if (existing.Status != RegistrationStatus.Cancelled)
                return ServiceResult<EventRegistrationDto>.Conflict("User is already registered for this event.");

            existing.Status = RegistrationStatus.Registered;
            existing.RegisteredAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            await InvalidateEventAsync(eventId, ct);

            return ServiceResult<EventRegistrationDto>.Ok(ToDto(existing));
        }

        var registration = new EventRegistration
        {
            EventId = eventId,
            UserId = userId,
            Status = RegistrationStatus.Registered,
            RegisteredAt = DateTimeOffset.UtcNow
        };

        db.EventRegistrations.Add(registration);
        await db.SaveChangesAsync(ct);
        await InvalidateEventAsync(eventId, ct);

        return ServiceResult<EventRegistrationDto>.Ok(ToDto(registration));
    }

    public async Task<ServiceResult<EventRegistrationDto>> UpdateStatusAsync(
        Guid eventId, Guid userId, string status, CancellationToken ct = default)
    {
        if (!RegistrationStatus.IsValid(status))
            return ServiceResult<EventRegistrationDto>.Invalid(
                $"Status must be one of: {string.Join(", ", RegistrationStatus.All)}.");

        var registration = await db.EventRegistrations
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId, ct);

        if (registration is null)
            return ServiceResult<EventRegistrationDto>.NotFound(
                $"No registration found for user '{userId}' on event '{eventId}'.");

        registration.Status = status.ToLowerInvariant();
        await db.SaveChangesAsync(ct);
        await InvalidateEventAsync(eventId, ct);

        return ServiceResult<EventRegistrationDto>.Ok(ToDto(registration));
    }

    public async Task<bool> CancelAsync(Guid eventId, Guid userId, CancellationToken ct = default)
    {
        var updated = await db.EventRegistrations
            .Where(r => r.EventId == eventId && r.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, RegistrationStatus.Cancelled), ct);

        if (updated > 0) await InvalidateEventAsync(eventId, ct);
        return updated > 0;
    }

    // The cached event DTO carries a registration count, so it goes stale on
    // any registration change.
    private Task InvalidateEventAsync(Guid eventId, CancellationToken ct) =>
        cache.RemoveAsync($"event:{eventId}", ct);

    private static EventRegistrationDto ToDto(EventRegistration r) =>
        new(r.EventId, r.UserId, r.Status, r.RegisteredAt);
}
