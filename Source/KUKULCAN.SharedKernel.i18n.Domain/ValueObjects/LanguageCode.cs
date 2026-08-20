using System.Text.RegularExpressions;
using KUKULCAN.SharedKernel.Results;

namespace KUKULCAN.SharedKernel.i18n.Domain.ValueObjects;

/// <summary>
/// Represents a BCP-47 language tag used to identify a locale
/// (e.g., <c>es-ES</c>, <c>en-US</c>, <c>ca-ES</c>, <c>fr-FR</c>).
/// </summary>
public sealed class LanguageCode : ValueObject
{
    private static readonly Regex _bcp47 = new(
        @"^[a-zA-Z]{2,3}(-[a-zA-Z]{2,4})?(-[a-zA-Z0-9]{2,8})*$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(50));

    /// <summary>Gets the full normalized BCP-47 language tag.</summary>
    public string Value { get; }

    /// <summary>Gets the two or three letter language subtag.</summary>
    public string Language { get; }

    /// <summary>Gets the region/country subtag, or <c>null</c> if not specified.</summary>
    public string? Region { get; }

    private LanguageCode(string value, string language, string? region)
    {
        Value = value;
        Language = language;
        Region = region;
    }

    /// <summary>Creates a validated BCP-47 <see cref="LanguageCode"/> value object.</summary>
    public static Result<LanguageCode> Create(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return Result<LanguageCode>.Failure(I18nErrors.Validation("LanguageCode.Empty", "Language code must not be empty."));

        string normalised = tag.Trim();
        if (!_bcp47.IsMatch(normalised))
            return Result<LanguageCode>.Failure(I18nErrors.Validation("LanguageCode.Format.Invalid",
                $"'{tag}' is not a valid BCP-47 language tag (e.g., 'es-ES', 'en-US')."));

        string[] parts = normalised.Split('-');
        string language = parts[0].ToLowerInvariant();
        string? region = parts.Length > 1 ? parts[1].ToUpperInvariant() : null;
        string value = region is not null ? $"{language}-{region}" : language;

        return Result<LanguageCode>.Success(new LanguageCode(value, language, region));
    }

    /// <summary>
    /// Returns the fallback lookup chain for translation resolution.
    /// E.g., <c>es-MX</c> → [<c>es-MX</c>, <c>es</c>, <c>en</c>].
    /// </summary>
    public IReadOnlyList<string> FallbackChain
    {
        get
        {
            var chain = new List<string> { Value };
            if (Region is not null)
                chain.Add(Language);
            if (!chain.Contains("en", StringComparer.OrdinalIgnoreCase))
                chain.Add("en");
            return chain;
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Implicitly converts a <see cref="LanguageCode"/> to its string representation.</summary>
    public static implicit operator string(LanguageCode lc) => lc.Value;
}
