namespace KUKULCAN.SharedKernel.i18n.Domain.DTOs;

/// <summary>Represents locale formatting rules returned by the application layer.</summary>
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
    int CurrencyDecimalPlaces,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);
