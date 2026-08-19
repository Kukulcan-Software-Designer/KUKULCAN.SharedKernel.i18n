using KUKULCAN.SharedKernel.Abstractions;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Abstractions;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.Services;

/// <summary>Provides the current system time for the i18n infrastructure.</summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider, IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);

    /// <inheritdoc />
    public long UnixTimestampSeconds => UtcNow.ToUnixTimeSeconds();
}
