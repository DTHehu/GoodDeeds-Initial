namespace GoodDeedsApi.Models;

/// <summary>
/// The core transactional entity. Tied strictly to one organization.
/// </summary>
public class Event
{
    public Guid Id { get; set; }

    /// <summary>References Organizations(id).</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>Name of the event.</summary>
    public string Title { get; set; } = null!;

    /// <summary>Details, agenda, or requirements.</summary>
    public string? Description { get; set; }

    /// <summary>Physical address or virtual meeting link.</summary>
    public string? Location { get; set; }

    /// <summary>Exact start time. Timezone aware.</summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>Timezone aware. Must be strictly greater than StartTime.</summary>
    public DateTimeOffset EndTime { get; set; }

    /// <summary>Audit trail for event creation.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    public Organization Organization { get; set; } = null!;

    public ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
}
