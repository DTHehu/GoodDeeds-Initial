using Microsoft.AspNetCore.Identity;

namespace GoodDeedsApi.Models;

/// <summary>
/// Id, Email, PhoneNumber and PasswordHash come from IdentityUser.
/// Named AppUser because ControllerBase.User already means the ClaimsPrincipal.
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    public string Name { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
}
