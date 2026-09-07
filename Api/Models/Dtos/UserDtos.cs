using System.ComponentModel.DataAnnotations;

namespace GoodDeedsApi.Models.Dtos;

/// <summary>Kept separate from AppUser so the password hash cannot be serialized.</summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public OrganizationDto? Organization { get; set; }
}

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string ContactEmail { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? Description { get; set; }
}

public class UpdateUserRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = null!;

    [Phone]
    [StringLength(32)]
    public string? PhoneNumber { get; set; }
}
