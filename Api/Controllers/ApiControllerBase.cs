using System.Security.Claims;
using GoodDeedsApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace GoodDeedsApi.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Read from the token, so it cannot be forged. Prefer this over any user id
    /// taken from a request body.
    /// </summary>
    protected Guid? CurrentUserId
    {
        get
        {
            string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out Guid id) ? id : null;
        }
    }

    protected bool IsAdmin => User.IsInRole(Roles.Admin);

    /// <summary>True when acting on yourself, or an admin acting on anyone.</summary>
    protected bool CanActOnBehalfOf(Guid userId)
    {
        return IsAdmin || CurrentUserId == userId;
    }
}
