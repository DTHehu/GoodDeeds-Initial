using GoodDeedsApi.Data;
using GoodDeedsApi.Models;
using GoodDeedsApi.Models.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
    /// Returns the organization on success, or the reason it failed.
    /// </summary>
    public async Task<(IdentityResult Result, Organization? Organization)> RegisterAsync(
        OrganizationRegisterRequest request)
    {
        string contactEmail = request.ContactEmail.Trim().ToLowerInvariant();

        // Checked up front so a duplicate returns a readable message instead of
        // a unique-index violation from the database.
        if (await _db.Organizations.AnyAsync(org => org.ContactEmail == contactEmail))
        {
            return (Failed("DuplicateOrganizationEmail",
                $"An organization with contact email '{contactEmail}' already exists."), null);
        }

        await using IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync();

        AppUser owner = new()
        {
            UserName = request.Email,
            Email = request.Email,
            Name = request.Name.Trim()
        };

        IdentityResult created = await _userManager.CreateAsync(owner, request.Password);

        if (!created.Succeeded)
        {
            // Leaving without committing rolls the user insert back.
            return (created, null);
        }

        Organization organization = new()
        {
            // Set here rather than letting the database default fill it in, so
            // the id is known before SaveChanges and can be put on the owner.
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

        return (IdentityResult.Success, organization);
    }

    private static IdentityResult Failed(string code, string description) =>
        IdentityResult.Failed(new IdentityError { Code = code, Description = description });
}
