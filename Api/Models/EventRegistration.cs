namespace GoodDeedsApi.Models;

/// <summary>
/// Join table mapping Users to Events. Composite primary key of
/// (EventId, UserId), so a user can only hold one registration per event.
/// </summary>
public class EventRegistration
{
    /// <summary>References Events(id).</summary>
    public Guid EventId { get; set; }

    /// <summary>References Users(id).</summary>
    public Guid UserId { get; set; }

    /// <summary>Tracks state. See <see cref="RegistrationStatus"/>.</summary>
    public string Status { get; set; } = RegistrationStatus.Registered;

    /// <summary>Audit trail for when the user signed up.</summary>
    public DateTimeOffset RegisteredAt { get; set; }

    public Event Event { get; set; } = null!;

    public AppUser User { get; set; } = null!;
}
