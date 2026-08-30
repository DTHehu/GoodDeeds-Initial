using GoodDeedsApi.Models.Dtos;

namespace GoodDeedsApi.Services;

public interface IEventService
{
    Task<IReadOnlyList<EventDto>> GetAllAsync(Guid? organizationId = null, bool upcomingOnly = false, CancellationToken ct = default);

    Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<ServiceResult<EventDto>> CreateAsync(CreateEventRequest request, CancellationToken ct = default);

    Task<ServiceResult<EventDto>> UpdateAsync(Guid id, UpdateEventRequest request, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
