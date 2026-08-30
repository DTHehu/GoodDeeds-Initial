using System.Security.Claims;
using GoodDeedsApi.Data;
using GoodDeedsApi.Identity;
using GoodDeedsApi.Models;
using GoodDeedsApi.Services;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace GoodDeedsApi;

/// <summary>
/// The starting point of the whole application.
///
/// It reads top to bottom in two halves:
///   1. Everything before builder.Build() registers the services the app can use.
///   2. Everything after it sets up how an incoming request is handled.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ================================================================
        // PART 1 — Register services
        // ================================================================

        // ---------- Postgres, through Entity Framework ----------
        // Connection strings live in appsettings.Development.json.
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));

            if (builder.Environment.IsDevelopment())
            {
                // Puts the real SQL and its parameter values in the console.
                // Very useful while learning; never switch it on in production,
                // because parameter values can include passwords.
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        });

        // ---------- Redis ----------
        // InstanceName prefixes every key, so a key we call "user:123" is
        // actually stored in Redis as "app:user:123".
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetConnectionString("Redis");
            options.InstanceName = "app:";
        });

        // ---------- Identity: users, passwords, roles, login tokens ----------
        builder.Services
            .AddIdentityApiEndpoints<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                // Five wrong passwords locks the account for five minutes.
                // This is what makes guessing passwords impractical.
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

                // No email server is set up yet, so requiring confirmation
                // would lock everyone out. Turn on once email works.
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddRoles<AppRole>()
            .AddUserManager<AppUserManager>()
            .AddEntityFrameworkStores<AppDbContext>();

        builder.Services.Configure<BearerTokenOptions>(
            IdentityConstants.BearerScheme,
            options =>
            {
                options.BearerTokenExpiration = TimeSpan.FromHours(1);
                options.RefreshTokenExpiration = TimeSpan.FromDays(14);
            });

        // ---------- Authorization rules ----------
        // A "policy" is just a named rule. Naming them here means controllers
        // can say [Authorize(Policy = Policies.AdminOnly)] instead of repeating
        // the rule, and risking a typo, in every file.
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Admin))
            .AddPolicy(Policies.AuthenticatedUser, policy => policy.RequireAuthenticatedUser());

        // ---------- Our own services ----------
        // This is the dependency injection setup. Each line says "if any class
        // asks for one of these, build it for them."
        //
        // AddScoped means one instance per HTTP request, which is what you
        // almost always want for anything that touches the database.
        //
        // >>> ADD YOUR NEW SERVICES HERE <<<
        builder.Services.AddScoped<RedisCacheService>();
        builder.Services.AddScoped<UserService>();

        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();

        // ---------- CORS, for local development only ----------
        // The React dev server runs on a different port, which browsers treat
        // as a different site. This says it is allowed to call us.
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddCors(options =>
                options.AddPolicy("dev", policy => policy
                    .WithOrigins("http://localhost:5173")   // Vite default
                    .AllowAnyHeader()
                    .AllowAnyMethod()));
        }

        // ================================================================
        // PART 2 — Build the request pipeline
        // ================================================================

        var app = builder.Build();

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
            app.UseCors("dev");

            app.MapOpenApi();

            // A browsable list of every endpoint, at /scalar. Start here when
            // you want to try the API by hand.
            app.MapScalarApiReference(options => options
                .WithTitle("GoodDeeds API")
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient));
        }

        // NOTE: UseHttpsRedirection is intentionally NOT called outside of
        // Development. In the container the app listens on plain HTTP :8080 and
        // Caddy terminates TLS in front of it. Leaving it on in production
        // produces redirect loops or a startup warning about no HTTPS port.

        // Order matters here. Authentication works out WHO you are; then
        // authorization decides WHAT you may do. Swapping these two breaks
        // every [Authorize] attribute in the app.
        app.UseAuthentication();
        app.UseAuthorization();

        // Hooks up everything in the Controllers folder.
        app.MapControllers();

        // ---------- The login endpoints ----------
        // One line gives us the whole set of account endpoints that ASP.NET
        // Core Identity ships with, all under /api/auth:
        //   POST /api/auth/register                 create an account
        //   POST /api/auth/login                    get a token
        //   POST /api/auth/refresh                  get a fresh token
        //   GET  /api/auth/confirmEmail
        //   POST /api/auth/resendConfirmationEmail
        //   POST /api/auth/forgotPassword
        //   POST /api/auth/resetPassword
        //   POST /api/auth/manage/2fa
        //   GET  /api/auth/manage/info              read email and claims
        //   POST /api/auth/manage/info              change email or password
        app.MapGroup("/api/auth")
           .WithTags("Auth")
           .MapIdentityApi<AppUser>();

        // Identity does not include a logout endpoint, so here is one.
        app.MapPost("/api/auth/logout", async (SignInManager<AppUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithTags("Auth");

        // Handy for the front end: "who am I signed in as?"
        app.MapGet("/api/auth/me", (ClaimsPrincipal principal) => Results.Ok(new
        {
            id = principal.FindFirstValue(ClaimTypes.NameIdentifier),
            email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.Identity?.Name,
            roles = principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray()
        }))
        .RequireAuthorization()
        .WithTags("Auth");

        // A quick way to check both databases are reachable.
        app.MapGet("/health", async (AppDbContext db, RedisCacheService cache) =>
        {
            bool postgresUp = await db.Database.CanConnectAsync();

            await cache.SetAsync("health:ping", "pong");
            bool redisUp = await cache.GetAsync<string>("health:ping") == "pong";

            var result = new { postgres = postgresUp, redis = redisUp };

            return postgresUp && redisUp
                ? Results.Ok(result)
                : Results.Json(result, statusCode: 503);
        })
        .AllowAnonymous();

        // Creates the tables and seeds the roles if they are not there yet, so
        // an empty database just works. See Data/DbInitializer.cs.
        await DbInitializer.InitializeAsync(app);

        await app.RunAsync();
    }
}

/// <summary>
/// The names of our authorization rules, in one place so a typo becomes a
/// compiler error instead of a security hole.
/// </summary>
public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string AuthenticatedUser = "AuthenticatedUser";
}
