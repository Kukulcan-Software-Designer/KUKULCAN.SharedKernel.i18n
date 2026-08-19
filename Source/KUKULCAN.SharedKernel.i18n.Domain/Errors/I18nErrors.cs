using KUKULCAN.SharedKernel.Results;

namespace KUKULCAN.SharedKernel.i18n.Domain.Errors;

/// <summary>
/// Creates domain errors used by the i18n module.
/// </summary>
public static class I18nErrors
{
    /// <summary>Creates a validation error.</summary>
    public static Error Validation(string code, string message) => new(code, message);

    /// <summary>Creates a not-found error.</summary>
    public static Error NotFound(string code, string message) => new(code, message);

    /// <summary>Creates a conflict error.</summary>
    public static Error Conflict(string code, string message) => new(code, message);

    /// <summary>Creates an unauthorized error.</summary>
    public static Error Unauthorized(string code, string message) => new(code, message);

    /// <summary>Creates a forbidden error.</summary>
    public static Error Forbidden(string code, string message) => new(code, message);

    /// <summary>Creates an unexpected error.</summary>
    public static Error Unexpected(string code, string message) => new(code, message);
}
