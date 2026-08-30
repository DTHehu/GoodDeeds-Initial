using System.ComponentModel.DataAnnotations;

namespace GoodDeedsApi.Models.Dtos;

/// <summary>
/// Outbound shape. Deliberately has no PasswordHash, and no Identity
/// bookkeeping such as security stamps or lockout counters.
/// </summary>
public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string? PhoneNumber,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> Roles);

// Account creation is handled by Identity at POST /api/auth/register, so there
// is deliberately no CreateUserRequest here.

public record UpdateUserRequest(
    [Required, StringLength(200)] string Name,
    [Phone, StringLength(32)] string? PhoneNumber);
