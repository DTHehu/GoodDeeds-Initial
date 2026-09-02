using System.Security.Claims;
using GoodDeedsApi.Models;
using GoodDeedsApi.Models.Dtos;
using GoodDeedsApi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

// ControllerBase.SignIn also returns a type called SignInResult, so the
// Identity one needs a distinct name.
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

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
        AppUser user = new() { UserName = request.Email, Email = request.Email };

        IdentityResult result = await _userManager.CreateAsync(user, request.Password);

        return result.Succeeded ? Ok() : IdentityErrors(result);
    }

    /// <summary>Creates an organization and the account that owns it.</summary>
    [HttpPost("registerOrg")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterOrg([FromBody] OrganizationRegisterRequest request)
    {
        (IdentityResult result, Organization? organization) = await _organizations.RegisterAsync(request);

        if (!result.Succeeded)
        {
            return IdentityErrors(result);
        }

        return Ok(new
        {
            organizationId = organization!.Id,
            name = organization.Name,
            contactEmail = organization.ContactEmail
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Issue a bearer token rather than setting a cookie.
        _signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

        IdentitySignInResult result = await _signInManager.PasswordSignInAsync(
            request.Email, request.Password, isPersistent: false, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Problem(result.ToString(), statusCode: StatusCodes.Status401Unauthorized);
        }

        // The sign-in above already wrote the token JSON to the response.
        return new EmptyResult();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        ISecureDataFormat<AuthenticationTicket> protector =
            _bearerOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;

        AuthenticationTicket? ticket = protector.Unprotect(request.RefreshToken);

        // ValidateSecurityStampAsync also rejects tokens issued before a
        // password change.
        if (ticket?.Properties?.ExpiresUtc is not { } expiresUtc
            || DateTimeOffset.UtcNow >= expiresUtc
            || await _signInManager.ValidateSecurityStampAsync(ticket.Principal) is not AppUser user)
        {
            return Unauthorized();
        }

        ClaimsPrincipal principal = await _signInManager.CreateUserPrincipalAsync(user);

        return SignIn(principal, IdentityConstants.BearerScheme);
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me() => Ok(new
    {
        id = User.FindFirstValue(ClaimTypes.NameIdentifier),
        email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name,
        roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray()
    });

    /// <summary>Turns Identity's error list into a 400 with the same shape as model validation.</summary>
    private ActionResult IdentityErrors(IdentityResult result)
    {
        foreach (IdentityError error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }

        return ValidationProblem(ModelState);
    }
}
