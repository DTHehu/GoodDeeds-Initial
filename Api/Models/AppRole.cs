using Microsoft.AspNetCore.Identity;

namespace GoodDeedsApi.Models;

/// <summary>
/// A named group a user can belong to. Authorization policies are written
/// against these names. See <see cref="Roles"/> for the seeded values.
/// </summary>
public class AppRole : IdentityRole<Guid>
{
    public AppRole() { }

    public AppRole(string roleName) : base(roleName) { }
}

/// <summary>The roles seeded on startup. Referenced by [Authorize(Roles = ...)].</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Member = "Member";

    public static readonly string[] All = [Admin, Member];
}
