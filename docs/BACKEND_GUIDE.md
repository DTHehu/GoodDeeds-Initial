# GoodDeeds Backend Field Guide

The starting point for the API: a working login system, one worked example of a
controller and a service, and nothing else. Everything the app actually *does* is
yours to build on top.

Written for someone comfortable programming but new to C#, Entity Framework,
dependency injection, or Redis.

**Stack:** .NET 10 · ASP.NET Core · EF Core 10 · PostgreSQL 16 · Redis 7 · ASP.NET Core Identity

---

## Contents

1. [Run it](#1-run-it)
2. [What is already here](#2-what-is-already-here)
3. [The shape of a request](#3-the-shape-of-a-request)
4. [C# you will see](#4-c-you-will-see)
5. [Dependency injection](#5-dependency-injection)
6. [Entity Framework Core](#6-entity-framework-core)
7. [Redis](#7-redis)
8. [Authentication and authorization](#8-authentication-and-authorization)
9. [Database startup](#9-database-startup)
10. [Build your first feature](#10-build-your-first-feature)
11. [Endpoint reference](#11-endpoint-reference)
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

**Where things live.** API at `https://localhost:7134`. A browsable list of every
endpoint at `/scalar` — start there when you want to try things by hand. The React
dev server is expected at `http://localhost:5173`, the only origin CORS allows in
development.

**Development login.** A starter admin account is seeded automatically:
`admin@localhost` / `ChangeMe123`. It is created *only* when the environment is
Development, and the credentials live in `appsettings.Development.json`. Nothing
seeds an admin on a real server.

---

## 2. What is already here

This is deliberately a small project. Knowing what is done and what is not saves
you hunting for code that was never written.

**Done, and you should not need to touch it:**

- Registration, login, logout, password reset, two-factor, account lockout
- Roles (`Admin` and `Member`) and the authorization rules built on them
- Postgres and Redis connections, and a `/health` endpoint proving both work
- Automatic database setup on startup, including on a brand new database

**One worked example, to copy from:**

- `UsersController` + `UserService` — read and edit user profiles

**Your job:**

- Everything else. The database tables for organizations, events and event
  registrations already exist (see `Models/`), but nothing reads or writes them
  yet. Section 10 walks through adding the first one.

---

## 3. The shape of a request

Every request walks the same path. Each layer has one job and only knows about the
layer directly beneath it:

```
Browser ──▶ Middleware ──▶ Controller ──▶ Service ──┬──▶ Redis      (checked first, 5 min TTL)
            authn→authz     HTTP only    the rules  └──▶ Postgres   (source of truth)
```

Controllers never touch the database, and services never touch HTTP. That split is
why the same service can later be called from a controller, a background job, or a
test without changing it.

---

## 4. C# you will see

### Fields and constructors

Classes in this project hold what they need in `private readonly` fields, set once
in the constructor:

```csharp
public class UserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }
}
```

`readonly` means the field is assigned in the constructor and never reassigned
after that. The leading underscore is just a naming convention for "this is a
field, not a local variable."

### async / await

Database and network calls take milliseconds, which is a very long time. Rather
than blocking a thread while waiting, `await` releases it back to the server to
handle other requests and resumes when the answer arrives. A method using `await`
must be marked `async` and returns `Task` (nothing) or `Task<T>` (a value,
eventually).

Practical rule: if a method name ends in `Async`, put `await` in front of it.

### Records

DTOs are `record`s — classes where you declare only the data, and the compiler
writes the constructor and properties:

```csharp
public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string? PhoneNumber,
    DateTimeOffset CreatedAt,
    List<string> Roles);
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
| `id.Value` | Reads the value out of a nullable, once you have checked it is not null. |

### LINQ

A query language built into C#. `Where`, `OrderBy`, `Select`, `Join` chain over any
collection. The important part here: chained over an EF `DbSet`, they are
*translated into SQL* rather than run in C#.

---

## 5. Dependency injection

**Files:** `Program.cs`, `Services/`

### The problem it solves

A controller needs a service. That service needs a database connection and a cache.
If every class built its own dependencies, the controller would have to know how to
construct all of it, and changing any piece would mean editing every call site.

Dependency injection inverts that. A class *declares what it needs* in its
constructor, and something else supplies it. That something else is the **service
container**.

### Registration: teaching the container

Each service is registered as itself:

```csharp
// Program.cs
builder.Services.AddScoped<RedisCacheService>();
builder.Services.AddScoped<UserService>();
```

Read as: "when anything asks for a `UserService`, build one, and reuse it for the
rest of this request."

**When you add a service, add a line here too.** Forgetting is the single most
common mistake, and the error it produces is
`Unable to resolve service for type 'YourService'`.

> **Why no interfaces.** A one-line `IUserService` with exactly one implementation
> is indirection without a payoff: it doubles the number of files, and every "go to
> definition" lands on a signature instead of the code. Interfaces earn their keep
> when there is genuinely more than one implementation. If that day comes,
> extracting one is a small, mechanical change.

### Consumption: asking for what you need

```csharp
// Services/UserService.cs
public class UserService
{
    private readonly AppDbContext _db;
    private readonly RedisCacheService _cache;

    public UserService(AppDbContext db, RedisCacheService cache)
    {
        _db = db;
        _cache = cache;
    }
}
```

`UserService` never writes `new AppDbContext(...)`. It states what it needs, and
ASP.NET Core passes those in. Controllers do exactly the same thing:

```csharp
// Controllers/UsersController.cs
public class UsersController : ApiControllerBase
{
    private readonly UserService _users;

    public UsersController(UserService users)
    {
        _users = users;
    }
}
```

When a request arrives, ASP.NET Core sees the controller needs a `UserService`,
which itself needs an `AppDbContext` and a `RedisCacheService`, builds all three in
the right order, and hands you a finished object. You never write that wiring.

### Lifetimes — the part that causes bugs

Registration also decides *how long an instance lives*:

| Lifetime | One instance per… | Use it for |
| --- | --- | --- |
| `AddScoped` | HTTP request | Anything touching the database. Everything in this project. |
| `AddSingleton` | Process | Stateless, thread-safe helpers and config caches. |
| `AddTransient` | Injection | Cheap, stateless objects where sharing would be wrong. |

> **The classic mistake.** Never inject a *scoped* service into a *singleton*. The
> singleton is built once and captures that first request's `DbContext` forever —
> which is not thread-safe and will eventually be disposed underneath it.

This is why `DbInitializer` opens an explicit scope instead of resolving straight
off `app.Services`:

```csharp
// Data/DbInitializer.cs
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
```

Startup code is not inside a request, so no scope exists yet. Asking for a scoped
service without one throws. Creating a scope manually gives the `DbContext` a
defined beginning and end — `using` disposes it when the block exits.

### Testing without interfaces

The usual argument for interfaces is that they let you swap a fake in during tests.
Without them you have two options that work just as well. Mark a method `virtual`
and subclass it for a stub, or — better for anything touching the database — skip
the fake entirely and run against a real Postgres in a throwaway container. The
second gives you more confidence anyway, because it exercises the actual SQL EF
generates rather than a mock's idea of it.

---

## 6. Entity Framework Core

**Files:** `Data/AppDbContext.cs`, `Models/*.cs`, `Data/Migrations/`

EF Core is an ORM: you write C# classes and LINQ queries, and it generates the SQL.

### 1. Entities — one class per table

A plain class whose properties become columns:

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

**Navigation properties** are the important idea. `Organization` is not a column —
it is a pointer EF follows using the `OrganizationId` foreign key. It lets you write
`ev.Organization.Name` and lets EF turn that into a JOIN.

### 2. The DbContext — the session

`AppDbContext` is your handle on the database for one request. Each `DbSet<T>` is a
table you can query:

```csharp
// Data/AppDbContext.cs
public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
}
```

`OnModelCreating` is where schema rules live — lengths, indexes, relationships,
constraints:

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

That check constraint is worth calling out. Your service layer should validate this
too, but the database is the last line of defence — a bad migration, a manual
`UPDATE`, or a future code path cannot slip in an event that ends before it starts.

### 3. Migrations — versioned schema changes

You never write `CREATE TABLE`. You change the C# model, and EF diffs it against a
snapshot of the last known schema:

```bash
# after changing an entity or OnModelCreating
dotnet ef migrations add DescribeYourChange --output-dir Data/Migrations

# apply it (or just run the app)
dotnet ef database update
```

Each migration has `Up` (apply) and `Down` (roll back). Applied names are recorded
in `__EFMigrationsHistory`, which is how EF knows what is left to run.

Two exist: `InitialCreate` builds the four schema tables; `AddIdentityAuth` adds the
six Identity tables and the extra user columns.

> **Read the generated SQL.** Migrations are generated, not sacred. Open the file
> before applying it — that check takes ten seconds and occasionally saves a table.

### Querying: LINQ becomes SQL

```csharp
// Services/UserService.cs
return query
    .AsNoTracking()
    .Select(user => new UserDto(
        user.Id,
        user.Name,
        user.Email!,
        user.PhoneNumber,
        user.CreatedAt,
        _db.UserRoles
            .Where(userRole => userRole.UserId == user.Id)
            .Join(_db.Roles,
                  userRole => userRole.RoleId,
                  role => role.Id,
                  (userRole, role) => role.Name!)
            .ToList()));
```

Nothing executes until someone calls `ToListAsync()` or `FirstOrDefaultAsync()`. Up
to that point EF is building a description of the query, which it then turns into a
**single** SQL statement — roles included, as a `LEFT JOIN`.

> **The N+1 trap.** If you instead loop over users and `await` a roles lookup for
> each one, you get one query per user. Twenty users, twenty-one round trips. Build
> one query and let the database do the join. `BuildUserQuery` in `UserService`
> exists exactly so that query can be reused by every read.

**Two more habits worth copying:**

- **`AsNoTracking()` on reads.** By default EF snapshots every row it returns so it
  can detect your edits. For read-only queries that is pure overhead.
- **`Select` into a DTO, not the entity.** Generates `SELECT` of only the columns
  you need, and means entity internals like `PasswordHash` can never leak into JSON.

### Change tracking, when you do want it

```csharp
AppUser? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
user.Name = request.Name.Trim();
await _db.SaveChangesAsync();   // EF works out the UPDATE itself
```

No `UPDATE` is written. Because the entity is tracked (no `AsNoTracking` here), EF
compares it to the original snapshot at `SaveChangesAsync` and issues a statement
with only the changed columns.

---

## 7. Redis

**Files:** `Services/RedisCacheService.cs`, `Program.cs`

### What Redis actually is

An in-memory key-value store. You give it a string key, it hands back a value, and
because everything lives in RAM the answer arrives in well under a millisecond. It
is not a replacement for Postgres — it holds no relationships, enforces no
constraints, and by design forgets things. It is a place to put an answer you
already computed.

### Cache-aside, the pattern used here

`UserService.GetByIdAsync` is the one worked example. Look in the cache, fall back
to the database on a miss, then store the result for next time:

```csharp
string cacheKey = $"user:{id}";

// 1. Ask Redis first.
UserDto? cached = await _cache.GetAsync<UserDto>(cacheKey);
if (cached != null)
{
    return cached;
}

// 2. Miss — go to Postgres.
UserDto? user = await BuildUserQuery(_db.Users.Where(u => u.Id == id))
    .FirstOrDefaultAsync();

if (user == null)
{
    return null;
}

// 3. Remember it for next time.
await _cache.SetAsync(cacheKey, user);

return user;
```

### Key naming

Redis is one flat namespace, so keys are structured by convention: `user:{id}`.
`Program.cs` also sets `InstanceName = "app:"`, so the real key is `app:user:0a1b…`.
That prefix lets another application share the same Redis server without colliding.

### Invalidation — the only genuinely hard part

A cache is a second copy of the truth, and a second copy can be wrong. The rule:
**whenever you write, delete the key.** Do not try to update it in place — deleting
is simpler and cannot leave a half-written value.

```csharp
await _db.SaveChangesAsync();
await _cache.RemoveAsync($"user:{id}");   // next read repopulates from Postgres
```

> **When you add caching to your own service**, ask what else can change the thing
> you cached. If a cached DTO includes a count of related rows, then changing one of
> those rows makes the cache stale even though the main row never moved. Most cache
> bugs are a missing eviction, not a bad read.

### Expiry as a safety net

Every entry is written with a five-minute lifetime, so even a missed eviction
self-corrects within five minutes rather than lasting forever.

### A cache outage must not be an outage

Every Redis call is wrapped in try/catch. If Redis is down, a read logs a warning
and returns null, which the caller treats as a miss and goes to Postgres. The site
gets slower; it does not break.

### Looking inside

```bash
docker exec -it schoolproj-dev-redis-1 redis-cli -a devpassword

KEYS app:*                  # every key (fine locally, never in production)
TTL app:user:<id>           # seconds until it expires
HGET app:user:<id> data     # the cached JSON
FLUSHDB                     # wipe the cache
```

> **Why `HGET` and not `GET`.** ASP.NET Core's distributed cache stores each entry
> as a Redis *hash* with separate fields for payload and expiry metadata, not as a
> plain string. Running `GET` on one returns `WRONGTYPE`. That is expected.

---

## 8. Authentication and authorization

**Files:** `Program.cs`, `Models/AppUser.cs`, `Identity/AppUserManager.cs`,
`Controllers/ApiControllerBase.cs`

**Authentication** establishes identity — you present a password, you get a token.
**Authorization** is the separate decision about whether that identity may perform
an action. They run as two middleware steps, in that order, and the order is not
optional:

```csharp
app.UseAuthentication();   // reads the token, works out who you are
app.UseAuthorization();    // decides what you may do
```

### Why ASP.NET Core Identity

Identity is the framework's built-in membership system. It ships password hashing,
lockout, roles, tokens, email confirmation and two-factor — all the parts of auth
that are easy to get subtly and dangerously wrong. The user entity builds on it:

```csharp
// Models/AppUser.cs
public class AppUser : IdentityUser<Guid>
{
    public string Name { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
}
```

The class declares only these because `IdentityUser<Guid>` already provides `Id`,
`Email`, `PhoneNumber` and `PasswordHash`. It is named `AppUser` rather than `User`
because inside a controller `User` already means the signed-in `ClaimsPrincipal`.

### The default endpoints

One line mounts the entire set of account endpoints Identity ships with:

```csharp
app.MapGroup("/api/auth")
   .WithTags("Auth")
   .MapIdentityApi<AppUser>();
```

Two more were added by hand: `logout` and `me`.

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

Access tokens last one hour; refresh tokens last fourteen days.

### Roles and policies

Two roles are seeded: `Admin` and `Member`. A **policy** is a named authorization
rule, declared once so controllers reference a constant instead of a magic string:

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Admin))
    .AddPolicy(Policies.AuthenticatedUser, policy => policy.RequireAuthenticatedUser());
```

Then in a controller:

```csharp
[Authorize(Policy = Policies.AuthenticatedUser)]   // on the class: applies to all actions
public class UsersController : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]       // on one action: tightens it further
    public async Task<ActionResult<List<UserDto>>> GetAll() { ... }
}
```

> **Gotcha — `[Authorize]` attributes stack.** A class-level attribute and an
> action-level one are combined with **AND**, not overridden. Putting `AdminOnly` on
> the class and `AuthenticatedUser` on one action does not widen that action — it
> still demands admin.
>
> So: make the class attribute the *loosest* rule that applies to any action, then
> tighten per action. Only `[AllowAnonymous]` genuinely overrides.

### Never trust an ID from the request body

If an endpoint took a user id from the JSON body, any signed-in user could act as
anyone. The caller's real identity comes from the token instead:

```csharp
// Controllers/ApiControllerBase.cs
protected Guid? CurrentUserId                  // read from the token, cannot be forged
protected bool IsAdmin
protected bool CanActOnBehalfOf(Guid userId)   // yourself, or admin acting on another
```

### Password rules and lockout

| Setting | Value | Why |
| --- | --- | --- |
| Minimum length | 8 | Plus one digit, one lower and one upper case. |
| Symbol required | no | Length beats symbol classes for real-world strength. |
| Lockout | 5 fails → 5 min | Makes online password guessing impractical. |
| Email confirmation | off | No SMTP yet; enabling it would lock out every new account. |

Passwords are stored as PBKDF2-HMAC-SHA512 hashes with 100,000 iterations.

> **401 vs 403.** `401 Unauthorized` means "I do not know who you are" — no token,
> or an expired one. `403 Forbidden` means "I know exactly who you are, and no."

---

## 9. Database startup

**Files:** `Data/DbInitializer.cs`

Pointing the API at an empty database is enough. On startup it runs four steps:

1. **Wait for Postgres.** Up to twelve attempts, five seconds apart, because the
   container is often still booting when the API starts.
2. **Apply migrations.** Everything not yet in `__EFMigrationsHistory`.
3. **Seed roles.** `Admin` and `Member`, skipped if already present.
4. **Seed a dev admin.** Development only, and only if `SeedAdmin` is configured.

Every step is idempotent — a second run applies nothing and seeds nothing.

> **Before this runs on a real server.** Migrate-on-startup is right for one
> instance and wrong for several: replicas racing to apply the same migration can
> deadlock. Once there is more than one API instance, move step 2 into a deploy step.

---

## 10. Build your first feature

Adding organizations end to end. The same four steps work for anything else.

**Step 1 — DTOs.** In `Models/Dtos/`, add `OrganizationDtos.cs` describing what goes
in and out. Keep it separate from the `Organization` entity.

```csharp
public record OrganizationDto(Guid Id, string Name, string ContactEmail);

public record CreateOrganizationRequest(
    [Required] [StringLength(200)] string Name,
    [Required] [EmailAddress] string ContactEmail);
```

**Step 2 — the service.** In `Services/`, add `OrganizationService.cs`. Copy the
shape of `UserService`: `private readonly` fields, a constructor that takes
`AppDbContext`, and methods returning DTOs or `null`.

```csharp
public class OrganizationService
{
    private readonly AppDbContext _db;

    public OrganizationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<OrganizationDto>> GetAllAsync()
    {
        return await _db.Organizations
            .AsNoTracking()
            .OrderBy(org => org.Name)
            .Select(org => new OrganizationDto(org.Id, org.Name, org.ContactEmail))
            .ToListAsync();
    }
}
```

**Step 3 — register it.** One line in `Program.cs`, under
`>>> ADD YOUR NEW SERVICES HERE <<<`:

```csharp
builder.Services.AddScoped<OrganizationService>();
```

Skip this and you get `Unable to resolve service for type 'OrganizationService'`.

**Step 4 — the controller.** In `Controllers/`, add `OrganizationsController.cs`
inheriting `ApiControllerBase`. Take the service in the constructor, and keep the
methods thin — call the service, turn the answer into a status code.

```csharp
[Route("api/organizations")]
[Authorize(Policy = Policies.AuthenticatedUser)]
public class OrganizationsController : ApiControllerBase
{
    private readonly OrganizationService _organizations;

    public OrganizationsController(OrganizationService organizations)
    {
        _organizations = organizations;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<OrganizationDto>>> GetAll()
    {
        return Ok(await _organizations.GetAllAsync());
    }
}
```

Run the app and check `/scalar` — your endpoints appear there automatically.

**A note on returning errors.** The example service returns `null` for "not found"
and the controller turns that into `NotFound()`. That is enough while a service has
one or two failure modes. If you later need to tell "not found" apart from "that
email is already taken," add a small enum or return a tuple — do not reach for
exceptions, which are slow and turn ordinary control flow into stack unwinding.

---

## 11. Endpoint reference

Browse them interactively at `/scalar`.

### Auth — all provided by Identity

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

### Users — the worked example

| Method | Path | Access |
| --- | --- | --- |
| GET | `/api/users` | **admin** |
| GET | `/api/users/me` | signed in |
| PUT | `/api/users/me` | signed in |
| GET | `/api/users/{id}` | self or admin |
| PUT | `/api/users/{id}` | self or admin |
| DELETE | `/api/users/{id}` | **admin** |
| GET | `/health` | anyone |

---

## 12. Loose ends

| Item | Where it stands |
| --- | --- |
| **Organizations, events, registrations** | Tables and entity classes exist; no services or controllers. Yours to build — see section 10. |
| **Password hash algorithm** | The schema said bcrypt or Argon2. Identity uses PBKDF2-HMAC-SHA512 at 100k iterations, which is OWASP-acceptable but a different algorithm. |
| **Extra user columns** | Identity adds `UserName`, normalized columns, security stamps, lockout and 2FA fields to `users` beyond the six the schema listed. That is the cost of not hand-rolling auth. |
| **Email delivery** | No SMTP configured, so `forgotPassword` and `confirmEmail` exist but send nothing. Email confirmation is off for that reason. |
| **Migrate on startup** | Correct for one instance. Move to a deploy step before running replicas. |
| **Automated tests** | None yet. Everything here was verified manually against the running stack. |
| **Schema PDF ambiguity** | The source PDF's table columns extract misaligned — constraints sit one row off their fields. The sensible reading was applied: `title` required, `description` and `location` nullable, `email` unique, `phone_number` nullable. |
