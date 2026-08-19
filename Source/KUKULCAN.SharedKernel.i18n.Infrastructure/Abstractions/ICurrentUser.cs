namespace KUKULCAN.SharedKernel.i18n.Infrastructure.Abstractions;

/// <summary>Provides the current authenticated user context to the i18n infrastructure.</summary>
public interface ICurrentUser
{
    /// <summary>Gets the user identifier.</summary>
    Guid UserId { get; }
    /// <summary>Gets the user name.</summary>
    string UserName { get; }
    /// <summary>Gets the user email.</summary>
    string? Email { get; }
    /// <summary>Gets the assigned roles.</summary>
    IReadOnlyList<string> Roles { get; }
    /// <summary>Gets the tenant identifier. i18n uses an empty value because its data is global.</summary>
    Guid TenantId { get; }
    /// <summary>Gets whether the request is authenticated.</summary>
    bool IsAuthenticated { get; }
    /// <summary>Determines whether the user has a role.</summary>
    bool IsInRole(string role);
    /// <summary>Determines whether the user has all specified roles.</summary>
    bool IsInAllRoles(params string[] roles);
}

/// <summary>Provides the system UTC time used by application infrastructure.</summary>
public interface IDateTimeProvider
{
    /// <summary>Gets the current UTC instant.</summary>
    DateTimeOffset UtcNow { get; }
    /// <summary>Gets the current UTC date.</summary>
    DateOnly Today { get; }
    /// <summary>Gets the current Unix timestamp in seconds.</summary>
    long UnixTimestampSeconds { get; }
}
