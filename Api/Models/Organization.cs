namespace GoodDeedsApi.Models;

public class Organization
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = null!;

    public string ContactEmail { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
