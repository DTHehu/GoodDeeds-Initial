using System.Security.Claims;
using GoodDeedsApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace GoodDeedsApi.Controllers;

/// <summary>
/// A shared base class for our controllers. It holds the few helpers that
/// every controller needs so they do not get copy-pasted around.
///
/// [ApiController] switches on some helpful ASP.NET Core behaviour, the most
/// useful being automatic model validation: if a request body breaks a rule
/// declared on the DTO (like [Required]), the framework returns 400 before
/// your method ever runs.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// The id of whoever is making this request, or null if nobody is signed in.
    ///
    /// This is read from the login token, which the client cannot tamper with.
    /// Always use this instead of taking a user id out of the request body —
    /// otherwise anyone could act as anyone else just by typing a different id.
    /// </summary>
    protected Guid? CurrentUserId
    {
        get
        {
            string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(value, out Guid id))
            {
                return id;
            }

            return null;
        }
    }

    /// <summary>True if the signed-in user is an administrator.</summary>
    protected bool IsAdmin => User.IsInRole(Roles.Admin);

    /// <summary>
    /// True when the caller is working on their own record, or is an admin
    /// working on somebody else's. Use it to stop ordinary users from reading
    /// or editing accounts that are not theirs.
    /// </summary>
    protected bool CanActOnBehalfOf(Guid userId)
    {
        return IsAdmin || CurrentUserId == userId;
    }
}
