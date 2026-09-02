using GoodDeedsApi.Data;
using GoodDeedsApi.Models;
using GoodDeedsApi.Models.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoodDeedsApi.Services;

public class OrganizationService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public OrganizationService(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    /// <summary>
    /// Creates an organization together with its first user account. Both are
    /// written in one transaction, so a failure part way through leaves neither
    /// behind.
    ///
    /// False means the contact email was taken, the login email was taken, or
    /// the password was rejected.
    /// </summary>
    public async Task<bool> RegisterAsync(OrganizationRegisterRequest request)
    {
        var contactEmail = request.ContactEmail.Trim().ToLower();

        if (await _db.Organizations.AnyAsync(org => org.ContactEmail == contactEmail))
        {
            return false;
        }

        //Makes the whole method atomic
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var owner = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            Name = request.Name.Trim()
        };

        if (!(await _userManager.CreateAsync(owner, request.Password)).Succeeded)
        {
            return false;
        }

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            ContactEmail = contactEmail,
            PhoneNumber = request.PhoneNumber.Trim(),
            Description = request.Description,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Organizations.Add(organization);
        owner.OrganizationId = organization.Id;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return true;
    }
}
