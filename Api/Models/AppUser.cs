using Microsoft.AspNetCore.Identity;

namespace GoodDeedsApi.Models;

/// <summary>
/// The global pool of users who can browse and register for events.
///
/// Inherits from IdentityUser&lt;Guid&gt;, which already supplies the Id, Email,
/// PhoneNumber and PasswordHash columns the schema calls for, plus the
/// bookkeeping ASP.NET Core Identity needs (UserName, security stamps, lockout
/// counters, 2FA flags). That is why this type only declares the two fields
/// Identity does not already have.
///
/// It is named AppUser rather than User because inside a controller the word
/// "User" already means ControllerBase.User, the signed-in ClaimsPrincipal.
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    /// <summary>User's display name.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Audit trail for account creation.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
}
