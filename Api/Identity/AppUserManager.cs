using GoodDeedsApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GoodDeedsApi.Identity;

/// <summary>
/// UserManager is the service Identity uses for every user operation: creating
/// accounts, hashing and verifying passwords, managing roles and lockouts.
/// MapIdentityApi's /register endpoint resolves it from DI and calls CreateAsync.
///
/// The built-in /register body is only { email, password }, but the schema
/// requires a non-null display name. Overriding CreateAsync lets the default
/// endpoint keep working while still guaranteeing Name and CreatedAt are set.
/// Users can change their display name afterwards via PUT /api/users/me.
/// </summary>
public class AppUserManager(
    IUserStore<AppUser> store,
    IOptions<IdentityOptions> optionsAccessor,
    IPasswordHasher<AppUser> passwordHasher,
    IEnumerable<IUserValidator<AppUser>> userValidators,
    IEnumerable<IPasswordValidator<AppUser>> passwordValidators,
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    IServiceProvider services,
    ILogger<UserManager<AppUser>> logger)
    : UserManager<AppUser>(store, optionsAccessor, passwordHasher, userValidators,
        passwordValidators, keyNormalizer, errors, services, logger)
{
    public override Task<IdentityResult> CreateAsync(AppUser user, string password)
    {
        ApplyDefaults(user);
        return base.CreateAsync(user, password);
    }

    public override Task<IdentityResult> CreateAsync(AppUser user)
    {
        ApplyDefaults(user);
        return base.CreateAsync(user);
    }

    private static void ApplyDefaults(AppUser user)
    {
        if (user.CreatedAt == default)
            user.CreatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(user.Name))
            return;

        // Fall back to the part of the email before the '@', so a registration
        // that never supplied a name still satisfies the not-null column.
        var email = user.Email ?? user.UserName ?? string.Empty;
        var localPart = email.Split('@')[0];

        user.Name = string.IsNullOrWhiteSpace(localPart) ? "New user" : localPart;
    }
}
