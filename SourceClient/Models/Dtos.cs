using System.Text.Json.Serialization;

namespace KUKULCAN.SharedKernel.i18n.Client.Models;

public record LanguageDto(
    string Code,
    string Name,
    string NativeName,
    bool IsActive,
    bool IsDefault,
    string? CreatedAt,
    string? UpdatedAt);

public record CreateLanguageRequest(string Code, string Name, string NativeName);
public record UpdateLanguageRequest(string Name, string NativeName);
public record SetActiveRequest(bool IsActive);

public record LocaleConfigurationDto(
    string LanguageCode,
    string DateFormat,
    string ShortDateFormat,
    string TimeFormat,
    string DateTimeFormat,
    string FirstDayOfWeek,
    string DecimalSeparator,
    string ThousandsSeparator,
    int DecimalPlaces,
    int CurrencyDecimalPlaces);

public record UpsertLocaleRequest(
    string DateFormat,
    string ShortDateFormat,
    string TimeFormat,
    string DateTimeFormat,
    string FirstDayOfWeek,
    string DecimalSeparator,
    string ThousandsSeparator,
    int DecimalPlaces = 2,
    int CurrencyDecimalPlaces = 2);

public record CurrencyFormatDto(
    string LanguageCode,
    string CurrencyCode,
    string CurrencyName,
    string Symbol,
    string SymbolPosition,
    bool SpaceBetweenSymbolAndAmount,
    string DecimalSeparator,
    string ThousandsSeparator,
    int DecimalPlaces,
    string NegativePattern,
    string FormattedExample);

public record UpsertCurrencyRequest(
    string CurrencyName,
    string Symbol,
    string SymbolPosition,
    bool SpaceBetweenSymbolAndAmount,
    string DecimalSeparator,
    string ThousandsSeparator,
    int DecimalPlaces,
    string NegativePattern = "-{symbol}{amount}");

public record TranslationDto(
    string Code,
    string LanguageCode,
    string Text,
    string? Context,
    int? MaxLength,
    bool IsReviewed,
    string? CreatedAt,
    string? UpdatedAt);

public record TranslationLookupDto(
    string Code,
    string LanguageCode,
    string Text,
    bool IsFallback,
    string ResolvedLanguageCode);

public record TranslationMapDto(
    string Module,
    string LanguageCode,
    [property: JsonPropertyName("translations")]
    Dictionary<string, string> Translations);

public record BulkUpsertResultDto(int Inserted, int Updated, int Skipped);

public record CreateTranslationRequest(
    string Code,
    string LanguageCode,
    string Text,
    string? Context = null,
    int? MaxLength = null);

public record UpdateTranslationRequest(string Text, string? Context = null);
public record SetReviewedRequest(bool IsReviewed);

public record BulkTranslationEntry(
    string Code,
    string LanguageCode,
    string Text,
    string? Context = null,
    int? MaxLength = null);

public record BulkUpsertRequest(IReadOnlyList<BulkTranslationEntry> Entries);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public record ApiError(string? Title, int Status, string? Detail);
