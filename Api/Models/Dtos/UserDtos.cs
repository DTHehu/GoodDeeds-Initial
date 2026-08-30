using System.ComponentModel.DataAnnotations;

namespace GoodDeedsApi.Models.Dtos;

/// <summary>Kept separate from AppUser so the password hash cannot be serialized.</summary>
public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string? PhoneNumber,
    DateTimeOffset CreatedAt,
    List<string> Roles);

public record UpdateUserRequest(
    [Required]
    [StringLength(200)]
    string Name,

    [Phone]
    [StringLength(32)]
    string? PhoneNumber);
