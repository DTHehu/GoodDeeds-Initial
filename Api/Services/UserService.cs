using GoodDeedsApi.Data;
using GoodDeedsApi.Models;
using GoodDeedsApi.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GoodDeedsApi.Services;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly RedisCacheService _cache;

    public UserService(AppDbContext db, RedisCacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        return await BuildUserQuery(_db.Users.OrderBy(user => user.Name))
            .ToListAsync();
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        string cacheKey = $"user:{id}";

        UserDto? cached = await _cache.GetAsync<UserDto>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        UserDto? user = await BuildUserQuery(_db.Users.Where(userInstance => userInstance.Id == id))
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return null;
        }

        await _cache.SetAsync(cacheKey, user);

        return user;
    }

    /// <summary>Returns null if there is no user with that id.</summary>
    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        AppUser? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return null;
        }

        // Email changes go through Identity at POST /api/auth/manage/info so the
        // normalized column and security stamp stay in sync.
        user.Name = request.Name.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();

        await _db.SaveChangesAsync();
        await _cache.RemoveAsync($"user:{id}");

        return await BuildUserQuery(_db.Users.Where(u => u.Id == id)).FirstAsync();
    }

    /// <summary>Returns false if there was no user with that id.</summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        AppUser? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return false;
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        await _cache.RemoveAsync($"user:{id}");

        return true;
    }

    // Returns a query, not results, so roles resolve as a join in the caller's
    // single statement rather than one lookup per user.
    private IQueryable<UserDto> BuildUserQuery(IQueryable<AppUser> query)
    {
        return query
            .AsNoTracking()
            .Select(user => new UserDto(
                user.Id,
                user.Name,
                user.Email!,
                user.PhoneNumber,
                user.CreatedAt,
                _db.UserRoles
                    .Where(userRole => userRole.UserId == user.Id)
                    .Join(_db.Roles,
                          userRole => userRole.RoleId,
                          role => role.Id,
                          (userRole, role) => role.Name!)
                    .ToList()));
    }
}
