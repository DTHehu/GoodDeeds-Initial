using System.ComponentModel.DataAnnotations;

namespace GoodDeedsApi.Models.Dtos;

public record EventDto(
    Guid Id,
    Guid OrganizationId,
    string Title,
    string? Description,
    string? Location,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    DateTimeOffset CreatedAt,
    int RegisteredCount);

public record CreateEventRequest(
    [Required] Guid OrganizationId,
    [Required, StringLength(300)] string Title,
    string? Description,
    [StringLength(500)] string? Location,
    [Required] DateTimeOffset StartTime,
    [Required] DateTimeOffset EndTime);

public record UpdateEventRequest(
    [Required, StringLength(300)] string Title,
    string? Description,
    [StringLength(500)] string? Location,
    [Required] DateTimeOffset StartTime,
    [Required] DateTimeOffset EndTime);
