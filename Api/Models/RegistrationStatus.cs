namespace GoodDeedsApi.Models;

/// <summary>
/// Allowed values for <see cref="EventRegistration.Status"/>. Stored as text in
/// Postgres so the set can grow without a schema change.
/// </summary>
public static class RegistrationStatus
{
    public const string Registered = "registered";
    public const string Cancelled = "cancelled";
    public const string Attended = "attended";
    public const string Waitlisted = "waitlisted";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Registered, Cancelled, Attended, Waitlisted
    };

    public static bool IsValid(string status) => All.Contains(status);
}
