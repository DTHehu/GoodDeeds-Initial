using GoodDeedsApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoodDeedsApi.Data;

/// <summary>
/// Brings an empty database up to a working state on startup so a new
/// contributor only has to run docker compose and press F5.
///
/// Runs three steps in order:
///   1. Wait for Postgres to accept connections (the container is often still
///      booting when the API starts).
///   2. Apply any migrations the database has not seen yet, which creates
///      every table from scratch on a brand new database.
///   3. Seed the rows the app cannot function without: the roles, and in
///      Development a starter admin account.
/// </summary>
public static class DbInitializer
{
    private const int MaxConnectionAttempts = 12;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    public static async Task InitializeAsync(WebApplication app)
    {
        // Services are resolved from a scope because DbContext and UserManager
        // are registered as scoped, and the application root provider is not a
        // scope. Resolving them directly from app.Services would throw.
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var logger = services.GetRequiredService<ILogger<Program>>();
        var db = services.GetRequiredService<AppDbContext>();

        await WaitForDatabaseAsync(db, logger);

        // Creates the database if it does not exist, then applies every
        // migration that has not been recorded in __EFMigrationsHistory.
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

        // Let the next call throw with the real provider error rather than a
        // generic message, so the cause is visible in the logs.
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
        // Only ever seeded locally. In any other environment the first admin is
        // promoted by hand, so a well-known password cannot ship to a server.
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
