using GoodDeedsApi.Data;
using GoodDeedsApi.Models;
using GoodDeedsApi.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GoodDeedsApi.Services;

public class OrganizationService(AppDbContext db, ICacheService cache) : IOrganizationService
{
    private static string CacheKey(Guid id) => $"organization:{id}";

    public async Task<IReadOnlyList<OrganizationDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Organizations
            .AsNoTracking()
            .OrderBy(o => o.Name)
            .Select(o => ToDto(o))
            .ToListAsync(ct);

    public async Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cached = await cache.GetAsync<OrganizationDto>(CacheKey(id), ct);
        if (cached is not null) return cached;

        var organization = await db.Organizations
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => ToDto(o))
            .FirstOrDefaultAsync(ct);

        if (organization is not null)
            await cache.SetAsync(CacheKey(id), organization, ct: ct);

        return organization;
    }

    public async Task<ServiceResult<OrganizationDto>> CreateAsync(
        CreateOrganizationRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.ContactEmail);

        if (await db.Organizations.AnyAsync(o => o.ContactEmail == email, ct))
            return ServiceResult<OrganizationDto>.Conflict($"An organization with contact email '{email}' already exists.");

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            ContactEmail = email,
            PhoneNumber = request.PhoneNumber?.Trim(),
            Description = request.Description,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Organizations.Add(organization);
        await db.SaveChangesAsync(ct);

        return ServiceResult<OrganizationDto>.Ok(ToDto(organization));
    }

    public async Task<ServiceResult<OrganizationDto>> UpdateAsync(
        Guid id, UpdateOrganizationRequest request, CancellationToken ct = default)
    {
        var organization = await db.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (organization is null)
            return ServiceResult<OrganizationDto>.NotFound($"Organization '{id}' was not found.");

        var email = Normalize(request.ContactEmail);

        if (await db.Organizations.AnyAsync(o => o.ContactEmail == email && o.Id != id, ct))
            return ServiceResult<OrganizationDto>.Conflict($"An organization with contact email '{email}' already exists.");

        organization.Name = request.Name.Trim();
        organization.ContactEmail = email;
        organization.PhoneNumber = request.PhoneNumber?.Trim();
        organization.Description = request.Description;

        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKey(id), ct);

        return ServiceResult<OrganizationDto>.Ok(ToDto(organization));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await db.Organizations.Where(o => o.Id == id).ExecuteDeleteAsync(ct);
        if (deleted > 0) await cache.RemoveAsync(CacheKey(id), ct);
        return deleted > 0;
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    private static OrganizationDto ToDto(Organization o) =>
        new(o.Id, o.Name, o.ContactEmail, o.PhoneNumber, o.Description, o.CreatedAt);
}
