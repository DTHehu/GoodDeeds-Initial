namespace GoodDeedsApi.Models.Dtos;

public class EventDto
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>Physical address or virtual meeting link.</summary>
    public string? Location { get; set; }

    public DateTimeOffset StartTime { get; set; }

    /// <summary>Must be strictly greater than StartTime.</summary>
    public DateTimeOffset EndTime { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}