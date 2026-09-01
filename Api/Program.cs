using System.Security.Claims;
using GoodDeedsApi.Data;
using GoodDeedsApi.Identity;
using GoodDeedsApi.Models;
using GoodDeedsApi.Services;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

namespace GoodDeedsApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));

            if (builder.Environment.IsDevelopment())
            {
                options.EnableDetailedErrors();

                // Logs parameter values, which include passwords.
                options.EnableSensitiveDataLogging();
            }
        });

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetConnectionString("Redis");
            options.InstanceName = "app:";
        });

        builder.Services
            .AddIdentityApiEndpoints<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

                // Turn on once SMTP is configured, or new accounts cannot sign in.
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

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Admin))
            .AddPolicy(Policies.AuthenticatedUser, policy => policy.RequireAuthenticatedUser());

        // Add new services here.
        builder.Services.AddScoped<RedisCacheService>();
        builder.Services.AddScoped<UserService>();

        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        // Declares the bearer scheme so Swagger UI shows an Authorize button and
        // sends the token with every request.
        builder.Services.AddOpenApi(options =>
            options.AddDocumentTransformer((document, context, ct) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        Description = "Paste the accessToken from POST /api/auth/login."
                    }
                };

                document.Security =
                [
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                    }
                ];

                return Task.CompletedTask;
            }));

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddCors(options =>
                options.AddPolicy("dev", policy => policy
                    .WithOrigins("http://localhost:5173")
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

            app.MapOpenApi();

            // Swagger UI at /swagger, reading /openapi/v1.json.
            app.UseSwaggerUI(options =>
                options.SwaggerEndpoint("/openapi/v1.json", "GoodDeeds API v1"));
        }

        // UseHttpsRedirection stays out of production: the container listens on
        // plain HTTP :8080 behind Caddy, and enabling it there causes redirect
        // loops.

        // Authentication must run before authorization.
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // register, login, refresh, confirmEmail, resendConfirmationEmail,
        // forgotPassword, resetPassword, manage/2fa, manage/info.
        app.MapGroup("/api/auth")
           .WithTags("Auth")
           .MapIdentityApi<AppUser>();

        // Identity ships no logout endpoint.
        app.MapPost("/api/auth/logout", async (SignInManager<AppUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithTags("Auth");

        app.MapGet("/api/auth/me", (ClaimsPrincipal principal) => Results.Ok(new
        {
            id = principal.FindFirstValue(ClaimTypes.NameIdentifier),
            email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.Identity?.Name,
            roles = principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray()
        }))
        .RequireAuthorization()
        .WithTags("Auth");

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

        await DbInitializer.InitializeAsync(app);

        await app.RunAsync();
    }
}

public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string AuthenticatedUser = "AuthenticatedUser";
}
