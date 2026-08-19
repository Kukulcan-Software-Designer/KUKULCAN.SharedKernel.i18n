namespace KUKULCAN.SharedKernel.i18n.Domain.DTOs;

/// <summary>Represents currency formatting rules returned by the application layer.</summary>
public record CurrencyFormatDto(
    Guid Id,
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
    string FormattedExample,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);
