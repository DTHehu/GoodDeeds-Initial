namespace GoodDeedsApi.Models;

/// <summary>
/// The primary entity hosting events. Isolated from user accounts.
/// </summary>
public class Organization
{
    public Guid Id { get; set; }

    /// <summary>Display name of the organization.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Primary contact; unique to prevent duplicates.</summary>
    public string ContactEmail { get; set; } = null!;

    /// <summary>Optional contact number.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Long-form details about the organization.</summary>
    public string? Description { get; set; }

    /// <summary>Audit trail for when the org was registered.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
