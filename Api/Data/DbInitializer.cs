using GoodDeedsApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoodDeedsApi.Data;

/// <summary>
/// Waits for Postgres, applies migrations, then seeds roles and a Development
/// admin, so an empty database comes up ready to use. Every step is idempotent.
/// </summary>
public static class DbInitializer
{
    private const int MaxConnectionAttempts = 12;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    public static async Task InitializeAsync(WebApplication app)
    {
        // Startup is not inside a request, so a scope has to be created before
        // scoped services can be resolved.
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var logger = services.GetRequiredService<ILogger<Program>>();
        var db = services.GetRequiredService<AppDbContext>();

        await WaitForDatabaseAsync(db, logger);

        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count > 0)
        {
            logger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
                pending.Count, string.Join(", ", pending));
        }

        await db.Database.MigrateAsync();

        await SeedRolesAsync(services, logger);
        await SeedAdminAsync(services, app.Configuration, app.Environment, logger);
    }

    private static async Task WaitForDatabaseAsync(AppDbContext db, ILogger logger)
    {
        for (var attempt = 1; attempt <= MaxConnectionAttempts; attempt++)
        {
            if (await db.Database.CanConnectAsync()) return;

            logger.LogWarning(
                "Database not reachable (attempt {Attempt}/{Max}). Retrying in {Delay}s. " +
                "Is the compose stack running? docker compose -f Docker/DevDBCompose.yaml up -d",
                attempt, MaxConnectionAttempts, RetryDelay.TotalSeconds);

            await Task.Delay(RetryDelay);
        }

        // Surfaces the provider's real error rather than a generic timeout.
        await db.Database.OpenConnectionAsync();
    }

    private static async Task SeedRolesAsync(IServiceProvider services, ILogger logger)
    {
        var roleManager = services.GetRequiredService<RoleManager<AppRole>>();

        foreach (var role in Roles.All)
        {
            if (await roleManager.RoleExistsAsync(role)) continue;

            var result = await roleManager.CreateAsync(new AppRole(role));
            if (result.Succeeded)
                logger.LogInformation("Seeded role {Role}", role);
            else
                logger.LogError("Could not seed role {Role}: {Errors}", role, Describe(result));
        }
    }

    private static async Task SeedAdminAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger)
    {
        // Development only, so a well-known password cannot reach a server.
        if (!environment.IsDevelopment()) return;

        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("SeedAdmin:Email / SeedAdmin:Password not configured. Skipping admin seed.");
            return;
        }

        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        if (await userManager.FindByEmailAsync(email) is not null) return;

        var admin = new AppUser
        {
            UserName = email,
            Email = email,
            Name = "Local Admin",
            EmailConfirmed = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var created = await userManager.CreateAsync(admin, password);
        if (!created.Succeeded)
        {
            logger.LogError("Could not seed admin user: {Errors}", Describe(created));
            return;
        }

        await userManager.AddToRoleAsync(admin, Roles.Admin);
        logger.LogWarning("Seeded DEVELOPMENT admin account {Email}. Local use only.", email);
    }

    private static string Describe(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
}
