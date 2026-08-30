using Microsoft.AspNetCore.Identity;

namespace GoodDeedsApi.Models;

public class AppRole : IdentityRole<Guid>
{
    public AppRole() { }

    public AppRole(string roleName) : base(roleName) { }
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Member = "Member";

    public static readonly string[] All = [Admin, Member];
}
