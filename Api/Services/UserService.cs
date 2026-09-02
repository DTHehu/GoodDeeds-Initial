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

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"user:{id}";

        var cached = await _cache.GetAsync<UserDto>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var user = await _db.Users.Where(userInstance => userInstance.Id == id).FirstOrDefaultAsync();

        if (user == null)
        {
            return null;
        }

        var organizationId = user.OrganizationId;
        var organizationDto = new OrganizationDto();
        if (organizationId != null)
        {
            var organizationEntity = await _db.Organizations.FirstOrDefaultAsync(organization => organization.Id == organizationId);

            if (organizationEntity != null)
            {
                organizationDto = new OrganizationDto()
                {
                    Id = organizationEntity.Id,
                    Name = organizationEntity.Name,
                    Description = organizationEntity.Description,
                    CreatedAt = organizationEntity.CreatedAt,
                    ContactEmail = organizationEntity.ContactEmail,
                    PhoneNumber = organizationEntity.PhoneNumber,
                };
            }
        }
        
        var userDto = new UserDto(user.Id, user.Name, user.Email, user.PhoneNumber, user.CreatedAt, organizationDto);
        
        await _cache.SetAsync(cacheKey, userDto);

        return userDto;
    }

    /// <summary>Returns null if there is no user with that id.</summary>
    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);

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

        var userEntity = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (userEntity == null)
        {
            return null;
        }
        
        return new UserDto(user.Id, user.Name, user.Email, user.PhoneNumber, user.CreatedAt, null);
    }

    /// <summary>Returns false if there was no user with that id.</summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return false;
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        await _cache.RemoveAsync($"user:{id}");

        return true;
    }
}
