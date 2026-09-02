using System.ComponentModel.DataAnnotations;

namespace GoodDeedsApi.Models.Dtos;

public record RegisterRequest(
    [Required]
    [EmailAddress]
    string Email,

    [Required]
    string Password,

    /// <summary>Display name shown on the volunteer's profile.</summary>
    [Required]
    [StringLength(200)]
    string Name);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public class OrganizationRegisterRequest
{
    /// <summary>Login email for the account that will own the organization.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;

    [Required]
    public string Name { get; set; } = null!;

    /// <summary>Public contact address for the organization. Must be unique.</summary>
    [Required]
    [EmailAddress]
    public string ContactEmail { get; set; } = null!;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = null!;

    [Required]
    public string Description { get; set; } = null!;
}
