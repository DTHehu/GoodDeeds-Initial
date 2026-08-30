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

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ---------- Postgres / EF Core ----------
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null));

            // Surfaces parameter values in logs. Local only; these are secrets
            // in any other environment.
            if (builder.Environment.IsDevelopment())
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        });

        // ---------- Redis ----------
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetConnectionString("Redis");
            options.InstanceName = "app:";
        });

        // ---------- Authentication / Identity ----------
        // AddIdentityApiEndpoints wires up the whole Identity stack and the
        // bearer-token scheme that MapIdentityApi's /login endpoint issues
        // tokens for. AddEntityFrameworkStores tells it to persist users and
        // roles through AppDbContext.
        builder.Services
            .AddIdentityApiEndpoints<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                // Five bad passwords locks the account for five minutes, which
                // is what makes online password guessing impractical.
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

                // No SMTP wired up yet, so requiring confirmation would lock
                // every new account out. Turn this on once mail is configured.
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

        // ---------- Authorization ----------
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Admin))
            // Any authenticated user. Named so intent reads clearly at the
            // call site instead of a bare [Authorize].
            .AddPolicy(Policies.AuthenticatedUser, policy => policy.RequireAuthenticatedUser());

        // ---------- Application services ----------
        builder.Services.AddScoped<RedisCacheService>();
        builder.Services.AddScoped<OrganizationService>();
        builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<EventService>();
        builder.Services.AddScoped<EventRegistrationService>();

        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();

        // ---------- CORS (dev only) ----------
        // In production Caddy serves the React build and the API from the same
        // origin, so no CORS is needed there. Locally they're on different
        // ports, so they're different origins.
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddCors(options =>
                options.AddPolicy("dev", policy => policy
                    .WithOrigins("http://localhost:5173")   // Vite default
                    .AllowAnyHeader()
                    .AllowAnyMethod()));
        }

        var app = builder.Build();

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
            app.UseCors("dev");

            // Serves the OpenAPI document at /openapi/v1.json
            app.MapOpenApi();

            // Scalar UI at /scalar
            app.MapScalarApiReference(options => options
                .WithTitle("GoodDeeds API")
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient));
        }

        // NOTE: UseHttpsRedirection is intentionally NOT called outside of
        // Development. In the container the app listens on plain HTTP :8080 and
        // Caddy terminates TLS in front of it. Leaving it on in production
        // produces redirect loops or a startup warning about no HTTPS port.

        // Order matters. Authentication works out who the caller is and must
        // run before authorization decides what they are allowed to do.
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // ---------- Identity's built-in endpoints ----------
        // Mounts the endpoints Identity ships with under /api/auth:
        //   POST /api/auth/register                 create an account
        //   POST /api/auth/login                    exchange credentials for a token
        //   POST /api/auth/refresh                  swap a refresh token for a fresh one
        //   GET  /api/auth/confirmEmail             confirm from an emailed link
        //   POST /api/auth/resendConfirmationEmail
        //   POST /api/auth/forgotPassword           send a reset code
        //   POST /api/auth/resetPassword            complete the reset
        //   POST /api/auth/manage/2fa               configure two-factor auth
        //   GET  /api/auth/manage/info              read email and claims
        //   POST /api/auth/manage/info              change email or password
        app.MapGroup("/api/auth")
           .WithTags("Auth")
           .MapIdentityApi<AppUser>();

        // Identity does not ship a logout endpoint. Bearer tokens are
        // stateless, so the client discards them; this clears the cookie for
        // callers using the cookie scheme instead.
        app.MapPost("/api/auth/logout", async (SignInManager<AppUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithTags("Auth");

        // Convenience endpoint for a SPA to ask who the caller is.
        app.MapGet("/api/auth/me", (ClaimsPrincipal principal) => Results.Ok(new
        {
            id = principal.FindFirstValue(ClaimTypes.NameIdentifier),
            email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.Identity?.Name,
            roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
        }))
        .RequireAuthorization()
        .WithTags("Auth");

        // Cheap liveness probe that proves both backing stores are reachable.
        app.MapGet("/health", async (AppDbContext db, RedisCacheService cache) =>
        {
            var postgresUp = await db.Database.CanConnectAsync();

            await cache.SetAsync("health:ping", "pong", TimeSpan.FromSeconds(10));
            var redisUp = await cache.GetAsync<string>("health:ping") == "pong";

            var payload = new { postgres = postgresUp, redis = redisUp };
            return postgresUp && redisUp ? Results.Ok(payload) : Results.Json(payload, statusCode: 503);
        })
        .AllowAnonymous();

        // Waits for Postgres, applies migrations, then seeds roles and (in
        // Development) an admin account, so a brand new database comes up ready
        // to use. See Data/DbInitializer.cs.
        await DbInitializer.InitializeAsync(app);

        await app.RunAsync();
    }
}

/// <summary>Policy names, kept in one place so controllers cannot typo them.</summary>
public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string AuthenticatedUser = "AuthenticatedUser";
}
