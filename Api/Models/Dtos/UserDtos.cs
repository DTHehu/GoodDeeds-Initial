using System.ComponentModel.DataAnnotations;

namespace GoodDeedsApi.Models.Dtos;

/// <summary>Kept separate from AppUser so the password hash cannot be serialized.</summary>
public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string? PhoneNumber,
    DateTimeOffset CreatedAt,
    OrganizationDto? Organization);

public class OrganizationDto
{
    public Guid Id { get; set; }
    public  string Name { get; set; }
    public  string ContactEmail { get; set; }
    public  string? PhoneNumber { get; set; }
    public  DateTimeOffset CreatedAt { get; set; }
    public string Description { get; set; }
    
}

public record UpdateUserRequest(
    [Required]
    [StringLength(200)]
    string Name,

    [Phone]
    [StringLength(32)]
    string? PhoneNumber);
