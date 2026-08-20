namespace KUKULCAN.SharedKernel.i18n.Domain.Errors;

/// <summary>Provides guard operations required by the i18n domain model.</summary>
public static class I18nGuard
{
    /// <summary>Ensures that a reference is not null and returns it.</summary>
    public static T Null<T>(T? value, string parameterName) where T : class
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        return value!;
    }

    /// <summary>Ensures that a string is not null, empty or whitespace and returns it.</summary>
    public static string NullOrWhiteSpace(string? value, string parameterName)
    {
        if (value is null)
            throw new ArgumentException("Value cannot be null.", parameterName);

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be empty or consist only of white-space characters.", parameterName);

        return value;
    }
}
