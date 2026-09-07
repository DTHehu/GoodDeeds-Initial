namespace GoodDeedsApi.Models.Dtos;

public class EventRegistrationDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTimeOffset RegisteredAt { get; set; }
}
