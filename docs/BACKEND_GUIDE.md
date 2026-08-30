# GoodDeeds Backend Field Guide

Everything that was added to the API, and why. Written for someone comfortable
programming but new to C#, Entity Framework, dependency injection, or Redis.

**Stack:** .NET 10 · ASP.NET Core · EF Core 10 · PostgreSQL 16 · Redis 7 · ASP.NET Core Identity

---

## Contents

1. [Run it](#1-run-it)
2. [The shape of a request](#2-the-shape-of-a-request)
3. [C# you will see](#3-c-you-will-see)
4. [Dependency injection](#4-dependency-injection)
5. [Entity Framework Core](#5-entity-framework-core)
6. [Redis](#6-redis)
7. [Authentication and authorization](#7-authentication-and-authorization)
8. [Database startup](#8-database-startup)
9. [Endpoint reference](#9-endpoint-reference)
10. [Every file, and why it exists](#10-every-file-and-why-it-exists)
11. [Three bugs found while building this](#11-three-bugs-found-while-building-this)
12. [Loose ends](#12-loose-ends)

---

## 1. Run it

Start the databases. They run in Docker, bound to `127.0.0.1` only, so nothing is
exposed to your network:

```bash
docker compose -f Docker/DevDBCompose.yaml up -d
```

Then start the API from `Api/`:

```bash
dotnet run --launch-profile https
```

That is the whole setup. You do **not** run a migration command by hand — the API
creates its own tables on startup. Confirm both stores are connected:

```bash
curl -k https://localhost:7134/health
# {"postgres":true,"redis":true}
```

**Where things live.** API at `https://localhost:7134`. Interactive API browser at
`/scalar`. The React dev server is expected at `http://localhost:5173`, which is the
only origin CORS allows in development.

**Development login.** A starter admin account is seeded automatically:
`admin@localhost` / `ChangeMe123`. It is created *only* when the environment is
Development, and the credentials live in `appsettings.Development.json`. Nothing
seeds an admin on a real server.

---

## 2. The shape of a request

Every request walks the same path. Each layer has one job and only knows about the
layer directly beneath it:

```
Browser ──▶ Middleware ──▶ Controller ──▶ Service ──┬──▶ Redis      (checked first, 5 min TTL)
            authn→authz     HTTP only    the rules  └──▶ Postgres   (source of truth)
```

Controllers never touch the database, and services never touch HTTP. That split is
why the same service can be called from a controller, a background job, or a test.

---

## 3. C# you will see

### Primary constructors

A class can declare constructor parameters right after its name, usable anywhere in
the body. These are equivalent:

```csharp
// Classic — what most tutorials show
public class UserService {
    private readonly AppDbContext db;
    public UserService(AppDbContext db) { this.db = db; }
    public Task DoThing() => db.Users.ToListAsync();
}

// Primary constructor — same thing, no boilerplate
public class UserService(AppDbContext db) {
    public Task DoThing() => db.Users.ToListAsync();
}
```

### Expression-bodied members

`=>` on a method means "this method is one expression, return it."
`public int Double(int x) => x * 2;` is the same as a body with `return x * 2;`.

### async / await

Database and network calls take milliseconds, which is a very long time. Rather than
blocking a thread while waiting, `await` releases it back to the server to handle
other requests and resumes when the answer arrives. A method using `await` must be
marked `async` and returns `Task` (nothing) or `Task<T>` (a value, eventually).

Practical rule: if a method name ends in `Async`, put `await` in front of it.

### Records

Every DTO is a `record` — a class where you declare only the data, and the compiler
writes the constructor, properties, and value-based equality:

```csharp
public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string? PhoneNumber,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> Roles);
```

### The question marks

Nullable reference types are on, so the compiler tracks what may be missing:

| Syntax | Meaning |
| --- | --- |
| `string Name` | Never null. Compiler warns if you might assign null. |
| `string? PhoneNumber` | May be null; check before using. |
| `= null!;` | "Trust me, EF fills this in." Suppresses the warning for framework-set values. |
| `user?.Name` | Evaluates to null instead of crashing if `user` is null. |
| `a ?? b` | Use `a` unless it is null, then `b`. |

### Pattern matching with `is`

Tests for null and assigns in one step:

```csharp
if (CurrentUserId is not { } id) return Unauthorized();
// past this line, `id` is a plain Guid, guaranteed not null
```

### LINQ

A query language built into C#. `Where`, `OrderBy`, `Select`, `Count` chain over any
collection. The important part here: chained over an EF `DbSet`, they are *translated
into SQL* rather than run in C#.

---

## 4. Dependency injection

**Files:** `Program.cs:90`, `Services/*`

### The problem it solves

A controller needs a service. That service needs a database context and a cache. If
every class built its own dependencies, the controller would need to know how to
construct all of it, and swapping any piece — for a test, or a different cache —
would mean editing every call site.

Dependency injection inverts that. A class *declares what it needs* and something
else supplies it. That something else is the **service container**.

### Registration: teaching the container

Each service is registered as itself. There is no interface in front of it:

```csharp
// Program.cs:90
builder.Services.AddScoped<RedisCacheService>();
builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<EventRegistrationService>();
```

Read as: "when anything asks for a `UserService`, build one, and reuse it for the
rest of this request."

> **Why no interfaces.** A one-line `IUserService` that only ever has a single
> implementation is indirection without a payoff: it doubles the number of files and
> means every "go to definition" lands on a signature instead of the code. Interfaces
> earn their keep when there is genuinely more than one implementation, or when a
> boundary has to be stubbed and cannot otherwise be. Neither applies here, so the
> concrete classes are registered directly. If a second implementation ever shows up,
> extracting an interface then is a small, mechanical change.

### Consumption: asking for what you need

```csharp
// Controllers/UsersController.cs
public class UsersController(
    UserService users,
    EventRegistrationService registrations) : ApiControllerBase
```

When a request arrives, ASP.NET Core sees the controller needs those two types, looks
up the registrations, builds a `UserService` — which itself needs an `AppDbContext`
and a `RedisCacheService`, so it builds those too — and hands the controller a
working object graph. You never write that wiring.

### Lifetimes — the part that causes bugs

| Lifetime | One instance per… | Use it for |
| --- | --- | --- |
| `AddScoped` | HTTP request | Anything touching the database. Everything in this project. |
| `AddSingleton` | Process | Stateless, thread-safe helpers and config caches. |
| `AddTransient` | Injection | Cheap, stateless objects where sharing would be wrong. |

> **The classic mistake.** Never inject a *scoped* service into a *singleton*. The
> singleton is built once and captures that first request's `DbContext` forever —
> which is not thread-safe and will eventually be disposed underneath it.

This is why `DbInitializer` opens an explicit scope instead of resolving straight off
`app.Services`:

```csharp
// Data/DbInitializer.cs
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
```

Startup code is not inside a request, so no scope exists yet. Asking for a scoped
service without one throws. Creating a scope manually gives the `DbContext` a defined
beginning and end — `using` disposes it when the block exits.

### Testing without interfaces

The usual argument for interfaces is that they let you swap a fake in during tests.
Without them you have two options that work just as well here. Mark a method
`virtual` and subclass it for a stub, or — better for anything touching the database
— skip the fake entirely and run against a real Postgres in a throwaway container.
The second gives you more confidence anyway, because it exercises the actual SQL EF
generates rather than a mock's idea of it.

---

## 5. Entity Framework Core

**Files:** `Data/AppDbContext.cs`, `Models/*.cs`, `Data/Migrations/`

EF Core is an ORM: you write C# classes and LINQ queries, and it generates the SQL.

### 1. Entities — one class per table

```csharp
// Models/Event.cs
public class Event
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTimeOffset StartTime { get; set; }

    // Navigation properties — relationships, not columns.
    public Organization Organization { get; set; } = null!;
    public ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
}
```

**Navigation properties** are the important idea. `Organization` is not a column — it
is a pointer EF follows using the `OrganizationId` foreign key. It lets you write
`ev.Organization.Name` and lets EF turn that into a JOIN.

### 2. The DbContext — the session

```csharp
// Data/AppDbContext.cs:15
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
}
```

`OnModelCreating` is where schema rules live — lengths, indexes, relationships,
constraints. Keeping it here rather than as attributes leaves the model classes free
of database concerns:

```csharp
entity.Property(e => e.Title).IsRequired().HasMaxLength(300);

// Deleting an org deletes its events too.
entity.HasOne(e => e.Organization)
      .WithMany(o => o.Events)
      .HasForeignKey(e => e.OrganizationId)
      .OnDelete(DeleteBehavior.Cascade);

// The database refuses bad data even if the app has a bug.
entity.ToTable(t => t.HasCheckConstraint(
    "ck_events_end_after_start", "\"EndTime\" > \"StartTime\""));
```

That check constraint is worth calling out. The service layer validates it too, but
the database is the last line of defence — a bad migration, a manual `UPDATE`, or a
future code path cannot slip in an event that ends before it starts.

### 3. Migrations — versioned schema changes

You never write `CREATE TABLE`. You change the C# model, and EF diffs it against a
snapshot of the last known schema:

```bash
# after changing an entity or OnModelCreating
dotnet ef migrations add DescribeYourChange --output-dir Data/Migrations

# apply it (or just run the app)
dotnet ef database update
```

Each migration has `Up` (apply) and `Down` (roll back). Applied names are recorded in
`__EFMigrationsHistory`, which is how EF knows what is left to run.

Two exist so far: `InitialCreate` builds the four schema tables; `AddIdentityAuth`
adds the six Identity tables and the extra user columns.

> **Read the generated SQL.** Migrations are generated, not sacred. When
> `AddIdentityAuth` was created, EF warned about possible data loss — reading it
> showed the cause was widening `PasswordHash` from `varchar(255)` to `text` and
> dropping a redundant index. Both harmless. That check takes ten seconds and
> occasionally saves a table.

### Querying: LINQ becomes SQL

```csharp
// Services/EventService.cs
await db.Events
    .AsNoTracking()
    .Where(e => e.OrganizationId == orgId)
    .OrderBy(e => e.StartTime)
    .Select(e => new EventDto(
        e.Id, e.Title, /* … */
        e.Registrations.Count(r => r.Status == RegistrationStatus.Registered)))
    .ToListAsync(ct);
```

Nothing executes until `ToListAsync`. Up to that point EF builds an expression tree,
then translates it into a single SQL statement — including the registration count,
which becomes a correlated subquery rather than a second round trip.

**Three habits worth copying:**

- **`AsNoTracking()` on reads.** By default EF snapshots every row it returns so it
  can detect your edits. For read-only queries that is pure overhead.
- **`Select` into a DTO, not the entity.** Generates `SELECT` of only the needed
  columns, and means entity internals like `PasswordHash` can never leak into JSON.
- **Pass the `CancellationToken`.** The `ct` threaded through every method carries
  "the client hung up," letting an abandoned query be cancelled.

### Change tracking, when you do want it

```csharp
// Services/UserService.cs
var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
user.Name = request.Name.Trim();
await db.SaveChangesAsync(ct);   // EF works out the UPDATE itself
```

No `UPDATE` is written. Because the entity is tracked, EF compares it to the original
snapshot at `SaveChangesAsync` and issues a statement with only the changed columns.

### Bulk operations skip the round trip

```csharp
// Services/EventRegistrationService.cs
await db.EventRegistrations
    .Where(r => r.EventId == eventId && r.UserId == userId)
    .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, RegistrationStatus.Cancelled), ct);
```

`ExecuteUpdateAsync` and `ExecuteDeleteAsync` emit one SQL statement directly, never
loading the row. Faster, but they bypass change tracking, so anything already loaded
in this request holds a stale copy.

---

## 6. Redis

**Files:** `Services/RedisCacheService.cs`, `Program.cs:39`

### What Redis actually is

An in-memory key-value store. You give it a string key, it hands back a value, and
because everything lives in RAM the answer arrives in well under a millisecond. It is
not a replacement for Postgres — it holds no relationships, enforces no constraints,
and by design forgets things. It is a place to put an answer you already computed.

### Cache-aside, the pattern used here

Look in the cache, fall back to the database on a miss, then store for next time:

```csharp
// Services/UserService.cs
public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
{
    // 1. Ask Redis first.
    var cached = await cache.GetAsync<UserDto>(CacheKey(id), ct);
    if (cached is not null) return cached;

    // 2. Miss — go to Postgres.
    var user = await Project(db.Users.AsNoTracking().Where(u => u.Id == id))
        .FirstOrDefaultAsync(ct);

    // 3. Remember it for next time.
    if (user is not null)
        await cache.SetAsync(CacheKey(id), user, ct: ct);

    return user;
}
```

### Key naming

Redis is one flat namespace, so keys are structured by convention: `user:{id}`,
`event:{id}`, `organization:{id}`. `Program.cs` also sets `InstanceName = "app:"`, so
the real key is `app:user:0a1b…`. That prefix lets another application share the same
Redis server without colliding.

### Invalidation — the only genuinely hard part

A cache is a second copy of the truth, and a second copy can be wrong. The rule here:
**whenever you write, delete the key.** Do not update it in place — deleting is
simpler and cannot leave a half-written value.

```csharp
await db.SaveChangesAsync(ct);
await cache.RemoveAsync(CacheKey(id), ct);   // next read repopulates from Postgres
```

There is a subtler case. The cached `EventDto` carries a `registeredCount`, so it goes
stale when a *registration* changes even though the event row did not. That is why the
registration service reaches over and evicts the event's key:

```csharp
// Services/EventRegistrationService.cs
private Task InvalidateEventAsync(Guid eventId, CancellationToken ct) =>
    cache.RemoveAsync($"event:{eventId}", ct);
```

> **The rule to remember.** When you add a field to a cached DTO, ask what else can
> change it. Anything that can must evict the key. Most cache bugs are a missing
> eviction, not a bad read.

### Expiry as a safety net

Every entry is written with a five-minute TTL, so even a missed eviction self-corrects
within five minutes rather than lasting forever:

```csharp
// Services/RedisCacheService.cs:10
private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);
```

### A cache outage must not be an outage

Every Redis call is wrapped in try/catch. If Redis is down, a read logs a warning and
returns "not found," which the caller treats as a miss and goes to Postgres. The site
gets slower; it does not break.

```csharp
// Services/RedisCacheService.cs:21
catch (Exception ex)
{
    // A cache outage should degrade to a database read, not a 500.
    logger.LogWarning(ex, "Redis read failed for key {CacheKey}", key);
    return default;
}
```

### Looking inside

```bash
docker exec -it schoolproj-dev-redis-1 redis-cli -a devpassword

KEYS app:*                  # every key (fine locally, never in production)
TTL app:user:<id>           # seconds until it expires
HGET app:user:<id> data     # the cached JSON
FLUSHDB                     # wipe the cache
```

> **Why `HGET` and not `GET`.** ASP.NET Core's distributed cache stores each entry as
> a Redis *hash* with separate fields for payload and expiry metadata, not as a plain
> string. Running `GET` on one returns `WRONGTYPE`. That is expected, not a bug.

---

## 7. Authentication and authorization

**Files:** `Program.cs:51`, `Models/AppUser.cs`, `Identity/AppUserManager.cs`,
`Controllers/ApiControllerBase.cs`

**Authentication** establishes identity — you present a password, you get a token.
**Authorization** is the separate decision about whether that identity may perform an
action. They run as two middleware steps, in that order, and the order is not
optional:

```csharp
// Program.cs:139
app.UseAuthentication();   // reads the token, builds ClaimsPrincipal
app.UseAuthorization();    // checks policies against it
```

### Why ASP.NET Core Identity

Identity is the framework's built-in membership system. It ships password hashing,
lockout, roles, tokens, email confirmation and two-factor — all the parts of auth
that are easy to get subtly and dangerously wrong. The existing `User` entity was
changed to build on it:

```csharp
// Models/AppUser.cs
public class AppUser : IdentityUser<Guid>
{
    public string Name { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
}
```

The class declares only two fields because `IdentityUser<Guid>` already provides
`Id`, `Email`, `PhoneNumber` and `PasswordHash` — four of the six columns the schema
asked for. It is renamed from `User` to `AppUser` because inside a controller `User`
already means the signed-in `ClaimsPrincipal`.

### The default endpoints

One line mounts the entire set of account endpoints Identity ships with:

```csharp
// Program.cs:155
app.MapGroup("/api/auth")
   .WithTags("Auth")
   .MapIdentityApi<AppUser>();
```

Two more were added by hand because Identity does not provide them: `logout` (bearer
tokens are stateless, so this only matters for cookie callers) and `me` for the SPA.

### How a token is obtained and used

```bash
# 1. Register
curl -k -X POST https://localhost:7134/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"you@example.com","password":"Hunter2Pass"}'

# 2. Log in — returns accessToken + refreshToken
curl -k -X POST https://localhost:7134/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"you@example.com","password":"Hunter2Pass"}'

# 3. Send it on every subsequent call
curl -k https://localhost:7134/api/users/me \
  -H "Authorization: Bearer <accessToken>"
```

Access tokens last one hour; refresh tokens last fourteen days. When the access token
expires the client posts its refresh token to `/api/auth/refresh` and gets a fresh
pair, so the user is not asked to log in again.

### Roles and policies

Two roles are seeded: `Admin` and `Member`. A **policy** is a named authorization
rule, declared once so controllers reference a constant instead of a magic string:

```csharp
// Program.cs:83
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Admin))
    .AddPolicy(Policies.AuthenticatedUser, policy => policy.RequireAuthenticatedUser());
```

> **Gotcha — `[Authorize]` attributes stack.** A controller-level `[Authorize]` and an
> action-level one are combined with **AND**, not overridden. Putting `AdminOnly` on
> the controller and `AuthenticatedUser` on one action does not widen that action — it
> still demands admin. This was a real bug here: ordinary users got `403` trying to
> sign themselves up for an event.
>
> The fix is to make the controller attribute the *loosest* rule that applies to any
> action, then tighten per action. Only `[AllowAnonymous]` genuinely overrides.

### Never trust an ID from the request body

The registration endpoint accepts an optional `userId`. Used directly, any signed-in
user could register anybody. The caller's real identity comes from the token instead:

```csharp
// Controllers/ApiControllerBase.cs
protected Guid? CurrentUserId =>
    Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

protected bool IsAdmin => User.IsInRole(Roles.Admin);

protected bool CanActOnBehalfOf(Guid userId) => IsAdmin || CurrentUserId == userId;
```

```csharp
// Controllers/EventsController.cs
var targetUserId = request?.UserId ?? callerId;
if (!CanActOnBehalfOf(targetUserId)) return Forbid();
```

Omit the body and you register yourself. Name someone else and you need to be admin.

### Password rules and lockout

| Setting | Value | Why |
| --- | --- | --- |
| Minimum length | 8 | Plus one digit, one lower and one upper case. |
| Symbol required | no | Length beats symbol classes for real-world strength. |
| Lockout | 5 fails → 5 min | Makes online password guessing impractical. |
| Email confirmation | off | No SMTP yet; enabling it would lock out every new account. |

Passwords are stored as PBKDF2-HMAC-SHA512 hashes with 100,000 iterations —
Identity's default, and a format that can be upgraded transparently on next login.

> **401 vs 403.** `401 Unauthorized` means "I do not know who you are" — no token, or
> an expired one. `403 Forbidden` means "I know exactly who you are, and no." A user
> hitting an admin endpoint gets 403, never 401.

---

## 8. Database startup

**Files:** `Data/DbInitializer.cs`, `Program.cs:197`

Pointing the API at an empty database is enough. On startup it runs four steps:

1. **Wait for Postgres.** Up to twelve attempts, five seconds apart. The container is
   often still booting when the API starts, and a clear "database not reachable,
   retrying" beats a stack trace.
2. **Apply migrations.** Every migration not yet in `__EFMigrationsHistory` runs,
   which on an empty database means all of them.
3. **Seed roles.** `Admin` and `Member`, skipped if already present.
4. **Seed a dev admin.** Development only, and only if `SeedAdmin` is configured.

```
Applying 2 pending migration(s): 20260830172012_InitialCreate, 20260830173531_AddIdentityAuth
Applying migration '20260830172012_InitialCreate'.
Applying migration '20260830173531_AddIdentityAuth'.
Seeded role Admin
Seeded role Member
Seeded DEVELOPMENT admin account admin@localhost. Local use only.
Application started.
```

Every step is idempotent — a second run applies nothing and seeds nothing. Verified by
creating a genuinely empty database, booting against it, and booting again.

> **Before this runs on a real server.** Migrate-on-startup is right for one instance
> and wrong for several: replicas racing to apply the same migration can deadlock.
> Once there is more than one API instance, move step 2 into a deploy step and leave
> the app doing only steps 1, 3 and 4.

---

## 9. Endpoint reference

Browse them interactively at `/scalar`.

### Auth

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| POST | `/api/auth/register` | anyone | Create an account |
| POST | `/api/auth/login` | anyone | Exchange credentials for tokens |
| POST | `/api/auth/refresh` | anyone | Swap a refresh token for a new pair |
| GET | `/api/auth/confirmEmail` | anyone | Confirm from an emailed link |
| POST | `/api/auth/resendConfirmationEmail` | anyone | Resend confirmation |
| POST | `/api/auth/forgotPassword` | anyone | Send a reset code |
| POST | `/api/auth/resetPassword` | anyone | Complete the reset |
| POST | `/api/auth/manage/2fa` | signed in | Configure two-factor |
| GET | `/api/auth/manage/info` | signed in | Read email and claims |
| POST | `/api/auth/manage/info` | signed in | Change email or password |
| POST | `/api/auth/logout` | signed in | Clear the cookie session |
| GET | `/api/auth/me` | signed in | Id, email and roles |

### Organizations, events, users

| Method | Path | Access |
| --- | --- | --- |
| GET | `/api/organizations` | anyone |
| GET | `/api/organizations/{id}` | anyone |
| POST | `/api/organizations` | **admin** |
| PUT | `/api/organizations/{id}` | **admin** |
| DELETE | `/api/organizations/{id}` | **admin** |
| GET | `/api/events?organizationId=&upcomingOnly=` | anyone |
| GET | `/api/events/{id}` | anyone |
| POST | `/api/events` | **admin** |
| PUT | `/api/events/{id}` | **admin** |
| DELETE | `/api/events/{id}` | **admin** |
| GET | `/api/events/{id}/registrations` | **admin** |
| POST | `/api/events/{id}/registrations` | signed in — self, or admin for others |
| PUT | `/api/events/{id}/registrations/{userId}` | **admin** |
| DELETE | `/api/events/{id}/registrations/{userId}` | self or admin |
| GET | `/api/users` | **admin** |
| GET | `/api/users/me` | signed in |
| PUT | `/api/users/me` | signed in |
| GET | `/api/users/{id}` | self or admin |
| PUT | `/api/users/{id}` | self or admin |
| DELETE | `/api/users/{id}` | **admin** |
| GET | `/api/users/{id}/events` | self or admin |
| GET | `/health` | anyone |

> **Registration status.** Cancelling is a soft delete — the row stays with status
> `cancelled` so the signup remains auditable, and re-registering reuses that row
> instead of colliding with the composite primary key. Valid values: `registered`,
> `cancelled`, `attended`, `waitlisted`.

---

## 10. Every file, and why it exists

### Models — the shape of the data

| File | Role |
| --- | --- |
| `Models/Organization.cs` | Host of events. Unique contact email. |
| `Models/AppUser.cs` | Identity user plus `Name` and `CreatedAt`. |
| `Models/AppRole.cs` | Role entity, and the `Roles` name constants. |
| `Models/Event.cs` | Belongs to exactly one organization. |
| `Models/EventRegistration.cs` | Join table. Composite key of (EventId, UserId). |
| `Models/RegistrationStatus.cs` | Allowed status values and validation. |
| `Models/Dtos/*.cs` | Request and response shapes, separate from entities. |

> **Why DTOs are separate from entities.** Three reasons. Entities carry things that
> must never be serialized — `PasswordHash`, security stamps. Entities have navigation
> properties that serialize into infinite loops. And a DTO lets the API shape stay
> stable while the table underneath changes.

### Data — persistence

| File | Role |
| --- | --- |
| `Data/AppDbContext.cs` | Table mapping, keys, indexes, relationships, constraints. |
| `Data/UtcDateTimeOffsetConverter.cs` | Normalizes every timestamp to UTC on write. |
| `Data/DbInitializer.cs` | Wait, migrate, seed roles, seed dev admin. |
| `Data/Migrations/` | Generated schema history. Two migrations so far. |

### Services — the business rules

| File | Role |
| --- | --- |
| `Services/ServiceResult.cs` | Lets a service say "not found" or "conflict" without throwing. |
| `Services/RedisCacheService.cs` | JSON wrapper over the distributed cache, fails soft. |
| `Services/OrganizationService.cs` | CRUD, duplicate-email detection. |
| `Services/UserService.cs` | Profile reads and updates, roles projection. |
| `Services/EventService.cs` | CRUD, filtering, time-order validation. |
| `Services/EventRegistrationService.cs` | Signup, cancel, re-register, status changes. |

**Why `ServiceResult` instead of exceptions.** A service needs to report "that
organization does not exist" or "that email is taken." Throwing for an expected
outcome is slow and turns ordinary control flow into stack unwinding. Instead a
service returns a result the controller maps to a status code in one place:

```csharp
// Controllers/ApiControllerBase.cs
protected ActionResult Failure<T>(ServiceResult<T> result) => result.Error switch
{
    ServiceError.NotFound   => NotFound(/* … */),
    ServiceError.Conflict   => Conflict(/* … */),
    ServiceError.Validation => BadRequest(/* … */),
    _ => throw new InvalidOperationException("Failure() called on a successful result.")
};
```

Every controller then reads the same way, and status codes stay consistent:

```csharp
return result.Succeeded ? Ok(result.Value) : Failure(result);
```

### Controllers and configuration

| File | Role |
| --- | --- |
| `Controllers/ApiControllerBase.cs` | Current-user helpers and error mapping. |
| `Controllers/OrganizationsController.cs` | Public reads, admin writes. |
| `Controllers/EventsController.cs` | Public reads, admin writes, user registrations. |
| `Controllers/UsersController.cs` | Profiles. No account creation — that is Identity's. |
| `Identity/AppUserManager.cs` | Fills in `Name` and `CreatedAt` at registration. |
| `Program.cs` | All wiring: DB, Redis, Identity, policies, services, routes. |
| `appsettings.Development.json` | Local connection strings and the dev admin seed. |
| `.gitignore` | Added at repo root — `bin/`, `obj/`, `.idea/`, `node_modules/`. |

> **Why `AppUserManager` exists.** Identity's built-in `/register` accepts only
> `{ email, password }`, but the schema requires a non-null display name. Overriding
> `CreateAsync` lets the stock endpoint keep working while guaranteeing `Name` gets
> filled — it defaults to the part of the email before the `@`, and the user can
> change it later at `PUT /api/users/me`.

---

## 11. Three bugs found while building this

All three were caught by running the code, not by reading it.

### 1 — Timestamps with a timezone offset crashed

The schema calls for timezone-aware times, and a client in Chicago naturally sends
`2026-09-15T09:00:00-05:00`. Npgsql refuses to write a `DateTimeOffset` to a
`timestamptz` column unless the offset is exactly UTC, so creating an event returned
`500`.

**Fix:** a value converter applied to every `DateTimeOffset` in the model, so no write
path can reintroduce it. Postgres stores `timestamptz` as an absolute instant and
never retains the original offset anyway, so nothing is lost — `09:00-05:00`
round-trips as `14:00Z`, the same moment.

```csharp
// Data/AppDbContext.cs:24
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    base.ConfigureConventions(configurationBuilder);

    configurationBuilder.Properties<DateTimeOffset>()
        .HaveConversion<UtcDateTimeOffsetConverter>();
}
```

### 2 — Users could not register themselves

`EventsController` carried `[Authorize(AdminOnly)]` at the class level, with
`[Authorize(AuthenticatedUser)]` on the registration action to widen it. Attributes
stack with AND, so the action still required admin and ordinary users got `403`.

**Fix:** the controller-level attribute is now the loosest rule that applies to any
action, and each admin action is tagged individually. The same latent trap was removed
from `OrganizationsController` before it could bite.

### 3 — A vulnerable transitive package

`Microsoft.AspNetCore.OpenApi` 10.0.6 pulled in `Microsoft.OpenApi` 2.0.0, which
carries a known high-severity advisory.

**Fix:** bumped to 10.0.11, which resolves `Microsoft.OpenApi` 2.7.5.
`dotnet list package --vulnerable --include-transitive` is now clean — worth running
before any release.

### Verified end to end

Both stores connect. Registration, login, refresh, lockout after five failed attempts,
role-gated endpoints, self-versus-admin registration, soft cancel and re-registration
all behave correctly. The cache read path was proven by editing a row directly in
Postgres, confirming the API still served the cached value, then evicting the key and
seeing the new one. A brand new database was created, booted against, and booted again
to confirm startup initialization is idempotent. All test data has since been removed.

---

## 12. Loose ends

| Item | Where it stands |
| --- | --- |
| **Password hash algorithm** | The schema said bcrypt or Argon2. Identity uses PBKDF2-HMAC-SHA512 at 100k iterations, which is OWASP-acceptable but a different algorithm. Changing it means writing a custom `IPasswordHasher`. |
| **Extra user columns** | Identity adds `UserName`, normalized columns, security stamps, lockout and 2FA fields to `users` beyond the six the schema listed. That is the cost of not hand-rolling auth. |
| **Email delivery** | No SMTP configured, so `forgotPassword` and `confirmEmail` exist but send nothing. Email confirmation is off for that reason. |
| **Organization ownership** | Any admin can edit any organization. If organizers should only manage their own, that needs an owner column and a resource-based policy. |
| **Migrate on startup** | Correct for one instance. Move to a deploy step before running replicas. |
| **Automated tests** | None yet. Everything above was verified manually against the running stack. |
| **Schema PDF ambiguity** | The source PDF's table columns extract misaligned — constraints sit one row off their fields. The sensible reading was applied: `title` required, `description` and `location` nullable, `email` unique, `phone_number` nullable. |
