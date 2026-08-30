using GoodDeedsApi.Data;
using GoodDeedsApi.Models;
using GoodDeedsApi.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GoodDeedsApi.Services;

/// <summary>
/// Business logic for users. Controllers handle HTTP; services like this one
/// do the actual work. Keeping them separate means this class can be called
/// from anywhere later (a controller, a background job, a test).
///
/// Use this as the template for the services you write next.
/// </summary>
public class UserService
{
    // These two fields hold the things this service needs to do its job.
    // "readonly" means they are set once, in the constructor, and never
    // reassigned afterwards.
    private readonly AppDbContext _db;
    private readonly RedisCacheService _cache;

    /// <summary>
    /// This is dependency injection in action. UserService never creates an
    /// AppDbContext or a RedisCacheService itself — it just declares that it
    /// needs them, and ASP.NET Core passes them in when it builds this object.
    ///
    /// The wiring that makes that happen is one line in Program.cs:
    ///     builder.Services.AddScoped&lt;UserService&gt;();
    /// </summary>
    public UserService(AppDbContext db, RedisCacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    /// <summary>Every user, sorted by name.</summary>
    public async Task<List<UserDto>> GetAllAsync()
    {
        return await BuildUserQuery(_db.Users.OrderBy(user => user.Name))
            .ToListAsync();
    }

    /// <summary>
    /// One user, or null if there is no user with that id.
    ///
    /// This is the one place in the boilerplate that uses Redis, so you can see
    /// the pattern. It is called "cache-aside": look in the cache, and only go
    /// to the database when the cache does not have the answer.
    /// </summary>
    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        string cacheKey = $"user:{id}";

        // 1. Ask Redis first. This is fast — well under a millisecond.
        UserDto? cached = await _cache.GetAsync<UserDto>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        // 2. Not in the cache, so ask Postgres.
        UserDto? user = await BuildUserQuery(_db.Users.Where(u => u.Id == id))
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return null;
        }

        // 3. Save the answer so the next request for this user skips step 2.
        await _cache.SetAsync(cacheKey, user);

        return user;
    }

    /// <summary>
    /// Updates a user's display name and phone number.
    /// Returns the updated user, or null if there is no user with that id.
    /// </summary>
    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        AppUser? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return null;
        }

        // Email is deliberately not editable here. Changing it has to go through
        // Identity so the normalized column and security stamp stay in sync:
        // POST /api/auth/manage/info
        user.Name = request.Name.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();

        // We never wrote an UPDATE statement. EF compared the object to how it
        // looked when it was loaded and works out the SQL itself.
        await _db.SaveChangesAsync();

        // The cached copy is now out of date, so throw it away. The next read
        // will rebuild it from the database.
        await _cache.RemoveAsync($"user:{id}");

        return await BuildUserQuery(_db.Users.Where(u => u.Id == id)).FirstAsync();
    }

    /// <summary>Deletes a user. Returns false if there was no such user.</summary>
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

    /// <summary>
    /// Describes how to turn user rows into the shape we send back over HTTP.
    /// Every read above goes through here, so the output is always consistent.
    ///
    /// This returns a query, not results. Nothing touches the database until
    /// the caller adds ToListAsync() or FirstOrDefaultAsync(), which lets EF
    /// turn the whole thing — including the role lookup — into ONE SQL
    /// statement instead of one query per user.
    ///
    /// Building a DTO by hand also matters for safety: AppUser carries the
    /// password hash and other Identity bookkeeping, and listing the fields
    /// explicitly guarantees none of that can be sent to a browser.
    /// </summary>
    private IQueryable<UserDto> BuildUserQuery(IQueryable<AppUser> query)
    {
        // AsNoTracking is a small speed-up for read-only queries. It tells EF
        // not to bother remembering these rows so it can detect edits later.
        return query
            .AsNoTracking()
            .Select(user => new UserDto(
                user.Id,
                user.Name,
                user.Email!,
                user.PhoneNumber,
                user.CreatedAt,
                // Role names live in a separate table, so look them up by
                // joining user_roles to roles.
                _db.UserRoles
                    .Where(userRole => userRole.UserId == user.Id)
                    .Join(_db.Roles,
                          userRole => userRole.RoleId,
                          role => role.Id,
                          (userRole, role) => role.Name!)
                    .ToList()));
    }
}
