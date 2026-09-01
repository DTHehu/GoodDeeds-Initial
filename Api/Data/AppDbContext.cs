using GoodDeedsApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GoodDeedsApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Maps the Identity types onto tables. Must run first.
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("organizations");
            entity.HasKey(o => o.Id);

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

            entity.Property(u => u.Name).IsRequired().HasMaxLength(200);
            entity.Property(u => u.CreatedAt).IsRequired().HasDefaultValueSql("now()");

            // Identity defaults these to 256; 320 is the real limit for an address.
            entity.Property(u => u.Email).IsRequired().HasMaxLength(320);
            entity.Property(u => u.NormalizedEmail).IsRequired().HasMaxLength(320);
            entity.Property(u => u.UserName).IsRequired().HasMaxLength(320);
            entity.Property(u => u.NormalizedUserName).IsRequired().HasMaxLength(320);
            entity.Property(u => u.PhoneNumber).HasMaxLength(32);

            // Identity's own index on this column is not unique.
            entity.HasIndex(u => u.NormalizedEmail).IsUnique().HasDatabaseName("IX_users_NormalizedEmail");
        });

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

            entity.HasOne(e => e.Organization)
                  .WithMany(o => o.Events)
                  .HasForeignKey(e => e.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.StartTime);

            entity.ToTable(t => t.HasCheckConstraint(
                "ck_events_end_after_start", "\"EndTime\" > \"StartTime\""));
        });

        modelBuilder.Entity<EventRegistration>(entity =>
        {
            entity.ToTable("event_registrations");

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
