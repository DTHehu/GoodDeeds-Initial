using GoodDeedsApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GoodDeedsApi.Data;

/// <summary>
/// Inherits IdentityDbContext rather than plain DbContext so ASP.NET Core
/// Identity can store its users, roles, claims, logins and tokens in the same
/// database and the same transaction as the application's own tables.
/// The three generic arguments say: use AppUser for users, AppRole for roles,
/// and Guid for both of their primary keys.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();

    // Users is already declared by IdentityDbContext as DbSet<AppUser>.

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Applies to every DateTimeOffset property in the model, so no write
        // path can reintroduce the non-UTC offset problem.
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<UtcDateTimeOffsetConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Must run first: this is what maps the Identity types onto tables.
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("organizations");
            entity.HasKey(o => o.Id);

            // Generated database side so inserts outside EF still get a UUID.
            entity.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(o => o.Name).IsRequired().HasMaxLength(200);
            entity.Property(o => o.ContactEmail).IsRequired().HasMaxLength(320);
            entity.Property(o => o.PhoneNumber).HasMaxLength(32);
            entity.Property(o => o.Description).HasColumnType("text");
            entity.Property(o => o.CreatedAt).IsRequired().HasDefaultValueSql("now()");

            entity.HasIndex(o => o.ContactEmail).IsUnique();
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users");

            // Id, Email, PhoneNumber and PasswordHash come from IdentityUser.
            // Identity generates the key in code, so no database default here.
            entity.Property(u => u.Name).IsRequired().HasMaxLength(200);
            entity.Property(u => u.CreatedAt).IsRequired().HasDefaultValueSql("now()");

            // Identity defaults these to 256; widened to the 320 characters an
            // email address is actually allowed to be.
            entity.Property(u => u.Email).IsRequired().HasMaxLength(320);
            entity.Property(u => u.NormalizedEmail).IsRequired().HasMaxLength(320);
            entity.Property(u => u.UserName).IsRequired().HasMaxLength(320);
            entity.Property(u => u.NormalizedUserName).IsRequired().HasMaxLength(320);
            entity.Property(u => u.PhoneNumber).HasMaxLength(32);

            // The schema calls for a unique email. Identity only creates a
            // non-unique index on NormalizedEmail by default.
            entity.HasIndex(u => u.NormalizedEmail).IsUnique().HasDatabaseName("IX_users_NormalizedEmail");
        });

        // Identity's own tables, renamed from the AspNet* defaults to match the
        // snake_case naming used by the rest of the schema.
        modelBuilder.Entity<AppRole>().ToTable("roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("events");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.OrganizationId).IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Location).HasMaxLength(500);
            entity.Property(e => e.StartTime).IsRequired();
            entity.Property(e => e.EndTime).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("now()");

            // Deleting an org takes its events with it; events cannot outlive
            // the organization that hosts them.
            entity.HasOne(e => e.Organization)
                  .WithMany(o => o.Events)
                  .HasForeignKey(e => e.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.StartTime);

            // Enforced in the database, not just in the service layer.
            entity.ToTable(t => t.HasCheckConstraint(
                "ck_events_end_after_start", "\"EndTime\" > \"StartTime\""));
        });

        modelBuilder.Entity<EventRegistration>(entity =>
        {
            entity.ToTable("event_registrations");

            // Composite key isolates the many-to-many and blocks duplicate
            // signups for the same (event, user) pair.
            entity.HasKey(r => new { r.EventId, r.UserId });

            entity.Property(r => r.Status)
                  .IsRequired()
                  .HasMaxLength(32)
                  .HasDefaultValue(RegistrationStatus.Registered);
            entity.Property(r => r.RegisteredAt).IsRequired().HasDefaultValueSql("now()");

            entity.HasOne(r => r.Event)
                  .WithMany(e => e.Registrations)
                  .HasForeignKey(r => r.EventId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.User)
                  .WithMany(u => u.Registrations)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => r.UserId);
        });
    }
}
