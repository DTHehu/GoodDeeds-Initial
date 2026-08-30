using GoodDeedsApi.Models.Dtos;

namespace GoodDeedsApi.Services;

public interface IOrganizationService
{
    Task<IReadOnlyList<OrganizationDto>> GetAllAsync(CancellationToken ct = default);

    Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<ServiceResult<OrganizationDto>> CreateAsync(CreateOrganizationRequest request, CancellationToken ct = default);

    Task<ServiceResult<OrganizationDto>> UpdateAsync(Guid id, UpdateOrganizationRequest request, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
