using GoodDeedsApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GoodDeedsApi.Identity;

/// <summary>
/// Identity's /register body is only { email, password }, but Name is not null
/// in the schema. Overriding CreateAsync fills it in so the stock endpoint keeps
/// working; users can change it later at PUT /api/users/me.
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

        var email = user.Email ?? user.UserName ?? string.Empty;
        var localPart = email.Split('@')[0];

        user.Name = string.IsNullOrWhiteSpace(localPart) ? "New user" : localPart;
    }
}
