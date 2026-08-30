using System.ComponentModel.DataAnnotations;

namespace GoodDeedsApi.Models.Dtos;

// A DTO ("data transfer object") is the shape of the JSON going in or out of
// the API. It is kept separate from the AppUser database entity on purpose:
// the entity holds the password hash and other Identity fields that must never
// be sent to a browser.
//
// These are "records", which are just classes where the compiler writes the
// constructor and properties for you from the list in brackets.

/// <summary>What we send back when someone asks about a user.</summary>
public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string? PhoneNumber,
    DateTimeOffset CreatedAt,
    List<string> Roles);

// Signing up is handled by Identity at POST /api/auth/register,
// so there is no "create user" request here.

/// <summary>
/// What we accept when someone edits a profile.
///
/// The [Required] and [StringLength] attributes are checked automatically
/// before your controller method runs. If they fail, the caller gets a 400
/// with a list of problems and you do not have to write any of that code.
/// </summary>
public record UpdateUserRequest(
    [Required]
    [StringLength(200)]
    string Name,

    [Phone]
    [StringLength(32)]
    string? PhoneNumber);
