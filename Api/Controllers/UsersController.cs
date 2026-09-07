using System.Security.Claims;
using GoodDeedsApi.Models.Dtos;
using GoodDeedsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoodDeedsApi.Controllers;

// Account creation lives in Identity at POST /api/auth/register.
[ApiController]
[Route("api/users")]
[Authorize(Policy = Policies.AuthenticatedUser)]
public class UsersController : ControllerBase
{
    private readonly UserService _users;

    public UsersController(UserService users)
    {
        _users = users;
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id) ? id : null;

    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateUserRequest request)
    {
        if (CurrentUserId == null)
        {
            return Unauthorized();
        }

        var updated = await _users.UpdateAsync(CurrentUserId.Value, request);

        return updated == null ? NotFound() : Ok(updated);
    }
}
