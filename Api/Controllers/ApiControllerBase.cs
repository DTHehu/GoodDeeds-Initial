using System.Security.Claims;
using GoodDeedsApi.Models;
using GoodDeedsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoodDeedsApi.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// The signed-in user's id, read from the token's NameIdentifier claim,
    /// or null when the request is anonymous. Always prefer this over a user
    /// id taken from the request body: the body is caller-controlled, the
    /// token is not.
    /// </summary>
    protected Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    protected bool IsAdmin => User.IsInRole(Roles.Admin);

    /// <summary>
    /// True when the caller is acting on their own record, or is an admin
    /// acting on someone else's.
    /// </summary>
    protected bool CanActOnBehalfOf(Guid userId) => IsAdmin || CurrentUserId == userId;

    /// <summary>
    /// Maps a failed <see cref="ServiceResult{T}"/> onto a problem response.
    /// Only call this when the result did not succeed.
    /// </summary>
    protected ActionResult Failure<T>(ServiceResult<T> result) => result.Error switch
    {
        ServiceError.NotFound => NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Not found",
            Detail = result.Message
        }),
        ServiceError.Conflict => Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = result.Message
        }),
        ServiceError.Validation => BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = result.Message
        }),
        _ => throw new InvalidOperationException("Failure() called on a successful result.")
    };
}
