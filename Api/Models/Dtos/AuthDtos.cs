using System.ComponentModel.DataAnnotations;

namespace GoodDeedsApi.Models.Dtos;

public record RegisterRequest(string Email, string Password);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public class OrganizationRegisterRequest
{
    [Required]
    public string Email { get; set; }
    
    [Required]
    public string Password { get; set; }
    
    [Required]
    public string Name { get; set; }
    
    [Required]
    public string ContactEmail { get; set; }
    
    [Required]
    [Phone]
    public string PhoneNumber { get; set; }
    
    [Required]
    public string Description { get; set; }
}
