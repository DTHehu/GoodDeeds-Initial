using System.ComponentModel.DataAnnotations;

namespace GoodDeedsApi.Models.Dtos;

public record EventRegistrationDto(
    Guid EventId,
    Guid UserId,
    string Status,
    DateTimeOffset RegisteredAt);

/// <summary>
/// UserId is optional and defaults to the caller. Supplying someone else's id
/// is an administrative action and is rejected for non-admins, so an ordinary
/// user cannot sign another account up for an event.
/// </summary>
public record RegisterForEventRequest(Guid? UserId = null);

public record UpdateRegistrationStatusRequest([Required, StringLength(32)] string Status);
