using GoodDeedsApi.Models.Dtos;

namespace GoodDeedsApi.Services;

public interface IEventRegistrationService
{
    Task<ServiceResult<IReadOnlyList<EventRegistrationDto>>> GetForEventAsync(Guid eventId, CancellationToken ct = default);

    Task<ServiceResult<IReadOnlyList<EventDto>>> GetEventsForUserAsync(Guid userId, CancellationToken ct = default);

    Task<ServiceResult<EventRegistrationDto>> RegisterAsync(Guid eventId, Guid userId, CancellationToken ct = default);

    Task<ServiceResult<EventRegistrationDto>> UpdateStatusAsync(Guid eventId, Guid userId, string status, CancellationToken ct = default);

    Task<bool> CancelAsync(Guid eventId, Guid userId, CancellationToken ct = default);
}
