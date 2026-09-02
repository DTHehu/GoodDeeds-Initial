using System.Security.Claims;
using GoodDeedsApi.Models;
using GoodDeedsApi.Models.Dtos;
using GoodDeedsApi.Services;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GoodDeedsApi.Controllers;

/// <summary>
/// Identity's MapIdentityApi is deliberately not used: it is all or nothing,
/// and would also map email confirmation, password reset and two-factor
/// endpoints that this project has no mail server for.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly OrganizationService _organizations;
    private readonly IOptionsMonitor<BearerTokenOptions> _bearerOptions;

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        OrganizationService organizations,
        IOptionsMonitor<BearerTokenOptions> bearerOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _organizations = organizations;
        _bearerOptions = bearerOptions;
    }

    /// <summary>Creates a volunteer account.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            Name = request.Name.Trim()
        };

        var created = await _userManager.CreateAsync(user, request.Password);

        if (!created.Succeeded)
        {
            // Surfaces the real reason: password too short, email taken, and so on.
            foreach (var error in created.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        return Ok();
    }

    /// <summary>Creates an organization and the account that owns it.</summary>
    [HttpPost("registerOrg")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterOrg([FromBody] OrganizationRegisterRequest request)
    {
        if (!await _organizations.RegisterAsync(request))
        {
            return BadRequest("Could not register the organization. The login email or "
                + "contact email may already be in use, or the password was rejected.");
        }

        return Ok();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Issue a bearer token rather than setting a cookie.
        _signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

        var signIn = await _signInManager.PasswordSignInAsync(
            request.Email, request.Password, isPersistent: false, lockoutOnFailure: true);

        if (!signIn.Succeeded)
        {
            return Problem(signIn.ToString(), statusCode: StatusCodes.Status401Unauthorized);
        }

        // The sign-in above already wrote the token JSON to the response.
        return new EmptyResult();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var protector = _bearerOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;

        var ticket = protector.Unprotect(request.RefreshToken);

        // ValidateSecurityStampAsync also rejects tokens issued before a
        // password change.
        if (ticket?.Properties?.ExpiresUtc is not { } expiresUtc
            || DateTimeOffset.UtcNow >= expiresUtc
            || await _signInManager.ValidateSecurityStampAsync(ticket.Principal) is not AppUser user)
        {
            return Unauthorized();
        }

        var principal = await _signInManager.CreateUserPrincipalAsync(user);

        return SignIn(principal, IdentityConstants.BearerScheme);
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name,
            roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray()
        });
    }
}
