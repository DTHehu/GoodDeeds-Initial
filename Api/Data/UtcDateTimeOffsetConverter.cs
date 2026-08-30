using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GoodDeedsApi.Data;

/// <summary>
/// Npgsql only writes a DateTimeOffset to timestamptz when the offset is zero,
/// but clients legitimately send offsets like -05:00. Postgres stores
/// timestamptz as an absolute instant and does not keep the original offset
/// anyway, so normalizing to UTC preserves the exact instant.
/// </summary>
public class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
{
    public UtcDateTimeOffsetConverter()
        : base(v => v.ToUniversalTime(), v => v.ToUniversalTime())
    {
    }
}
