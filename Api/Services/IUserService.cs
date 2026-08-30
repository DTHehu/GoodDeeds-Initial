using GoodDeedsApi.Models.Dtos;

namespace GoodDeedsApi.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct = default);

    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
