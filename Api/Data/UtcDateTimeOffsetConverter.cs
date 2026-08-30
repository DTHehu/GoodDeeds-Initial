using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GoodDeedsApi.Data;

/// <summary>
/// Npgsql only writes a DateTimeOffset to 'timestamp with time zone' when the
/// offset is zero, but clients legitimately send offsets like -05:00. Postgres
/// stores timestamptz as an absolute instant and does not retain the original
/// offset either way, so normalizing to UTC on write preserves the exact
/// instant and loses nothing. Reads already come back as UTC.
/// </summary>
public class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
{
    public UtcDateTimeOffsetConverter()
        : base(v => v.ToUniversalTime(), v => v.ToUniversalTime())
    {
    }
}
