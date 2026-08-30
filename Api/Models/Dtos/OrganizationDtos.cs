using System.ComponentModel.DataAnnotations;

namespace GoodDeedsApi.Models.Dtos;

public record OrganizationDto(
    Guid Id,
    string Name,
    string ContactEmail,
    string? PhoneNumber,
    string? Description,
    DateTimeOffset CreatedAt);

public record CreateOrganizationRequest(
    [Required, StringLength(200)] string Name,
    [Required, EmailAddress, StringLength(320)] string ContactEmail,
    [Phone, StringLength(32)] string? PhoneNumber,
    string? Description);

public record UpdateOrganizationRequest(
    [Required, StringLength(200)] string Name,
    [Required, EmailAddress, StringLength(320)] string ContactEmail,
    [Phone, StringLength(32)] string? PhoneNumber,
    string? Description);
