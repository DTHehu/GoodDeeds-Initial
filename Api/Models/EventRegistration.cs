namespace GoodDeedsApi.Models;

/// <summary>Composite key of (EventId, UserId): one registration per user per event.</summary>
public class EventRegistration
{
    public Guid EventId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>See <see cref="RegistrationStatus"/>.</summary>
    public string Status { get; set; } = RegistrationStatus.Registered;

    public DateTimeOffset RegisteredAt { get; set; }

    public Event Event { get; set; } = null!;

    public AppUser User { get; set; } = null!;
}
