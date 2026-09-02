using GoodDeedsApi.Data;
using GoodDeedsApi.Models;
using GoodDeedsApi.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GoodDeedsApi.Services;

public class OrganizationService
{
    public readonly AppDbContext _db;
    
    public OrganizationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task RegisterOrg(OrganizationRegisterRequest request, Guid userId)
    {
        var newOrgEntity = new Organization()
        {
            CreatedAt = DateTime.UtcNow,
            ContactEmail = request.ContactEmail,
            Description = request.Description,
            Name = request.Name,
            PhoneNumber = request.PhoneNumber
        };
        
        var initialUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (initialUser == null)
        {
            return;
        }
        
        initialUser.OrganizationId = newOrgEntity.Id;
        
        await _db.Organizations.AddAsync(newOrgEntity);
        await _db.SaveChangesAsync();
    }
}