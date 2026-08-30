using GoodDeedsApi.Data;
using GoodDeedsApi.Models;
using GoodDeedsApi.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GoodDeedsApi.Services;

public class UserService(AppDbContext db, ICacheService cache) : IUserService
{
    private static string CacheKey(Guid id) => $"user:{id}";

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct = default) =>
        await Project(db.Users.AsNoTracking().OrderBy(u => u.Name)).ToListAsync(ct);

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cached = await cache.GetAsync<UserDto>(CacheKey(id), ct);
        if (cached is not null) return cached;

        var user = await Project(db.Users.AsNoTracking().Where(u => u.Id == id)).FirstOrDefaultAsync(ct);

        if (user is not null)
            await cache.SetAsync(CacheKey(id), user, ct: ct);

        return user;
    }

    public async Task<ServiceResult<UserDto>> UpdateAsync(
        Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return ServiceResult<UserDto>.NotFound($"User '{id}' was not found.");

        // Email is not updated here. Changing it has to go through Identity so
        // the normalized column, the security stamp and confirmation state all
        // stay consistent: POST /api/auth/manage/info.
        user.Name = request.Name.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();

        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKey(id), ct);

        var updated = await Project(db.Users.AsNoTracking().Where(u => u.Id == id)).FirstAsync(ct);
        return ServiceResult<UserDto>.Ok(updated);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await db.Users.Where(u => u.Id == id).ExecuteDeleteAsync(ct);
        if (deleted > 0) await cache.RemoveAsync(CacheKey(id), ct);
        return deleted > 0;
    }

    // Role names live in the join table, so they are read back with a
    // correlated subquery rather than a second round trip per user.
    private IQueryable<UserDto> Project(IQueryable<AppUser> source) =>
        source.Select(u => new UserDto(
            u.Id,
            u.Name,
            u.Email!,
            u.PhoneNumber,
            u.CreatedAt,
            db.UserRoles
                .Where(ur => ur.UserId == u.Id)
                .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
                .ToList()));
}
