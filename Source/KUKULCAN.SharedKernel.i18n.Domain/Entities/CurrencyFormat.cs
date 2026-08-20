using KUKULCAN.SharedKernel.Guards;
using KUKULCAN.SharedKernel.i18n.Domain.Errors;
using KUKULCAN.SharedKernel.i18n.Domain.ValueObjects;
using KUKULCAN.SharedKernel.i18n.Domain.ValueObjects.Enums;
using KUKULCAN.SharedKernel.Results;

namespace KUKULCAN.SharedKernel.i18n.Domain.Entities;

/// <summary>
/// Defines how a currency is formatted for a specific language/locale.
/// </summary>
public sealed class CurrencyFormat : AuditableEntity<I18nEntityId>
{
    private CurrencyFormat() { }

    /// <summary>
    /// Creates a currency formatting definition.
    /// </summary>
    public static Result<CurrencyFormat> Create(
        Guid id,
        string languageCode,
        string currencyCode,
        string currencyName,
        string symbol,
        CurrencySymbolPosition symbolPosition,
        bool spaceBetweenSymbolAndAmount,
        char decimalSeparator,
        char thousandsSeparator,
        int decimalPlaces,
        string negativePattern = "-{symbol}{amount}")
    {
        var langResult = LanguageCode.Create(languageCode);
        if (langResult.IsFailure)
            return Result<CurrencyFormat>.Failure(langResult.Error);

        if (string.IsNullOrWhiteSpace(currencyCode))
            return Result<CurrencyFormat>.Failure(I18nErrors.Validation("CurrencyFormat.CurrencyCode.Empty", "Currency code must not be empty."));

        if (currencyCode.Trim().Length != 3 || currencyCode.Trim().Any(c => !char.IsLetter(c)))
            return Result<CurrencyFormat>.Failure(I18nErrors.Validation("CurrencyFormat.CurrencyCode.Invalid", "Currency code must be a 3-letter ISO 4217 code."));

        if (string.IsNullOrWhiteSpace(currencyName))
            return Result<CurrencyFormat>.Failure(I18nErrors.Validation("CurrencyFormat.CurrencyName.Empty", "Currency name must not be empty."));

        if (string.IsNullOrWhiteSpace(symbol))
            return Result<CurrencyFormat>.Failure(I18nErrors.Validation("CurrencyFormat.Symbol.Empty", "Currency symbol must not be empty."));

        if (decimalSeparator == thousandsSeparator)
            return Result<CurrencyFormat>.Failure(I18nErrors.Validation("CurrencyFormat.Separators.Conflict", "DecimalSeparator and ThousandsSeparator must be different characters."));

        if (decimalPlaces is < 0 or > 10)
            return Result<CurrencyFormat>.Failure(I18nErrors.Validation("CurrencyFormat.DecimalPlaces.OutOfRange", $"DecimalPlaces must be between 0 and 10. Got: {decimalPlaces}."));

        if (string.IsNullOrWhiteSpace(negativePattern) || !negativePattern.Contains("{amount}"))
            return Result<CurrencyFormat>.Failure(I18nErrors.Validation("CurrencyFormat.NegativePattern.Invalid", "NegativePattern must contain the {amount} placeholder."));

        return Result<CurrencyFormat>.Success(new CurrencyFormat
        {
            Id = new I18nEntityId(Guard.NotDefault(id, nameof(id))),
            LanguageCode = langResult.Value,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            CurrencyName = currencyName.Trim(),
            Symbol = symbol.Trim(),
            SymbolPosition = symbolPosition,
            SpaceBetweenSymbolAndAmount = spaceBetweenSymbolAndAmount,
            DecimalSeparator = decimalSeparator,
            ThousandsSeparator = thousandsSeparator,
            DecimalPlaces = decimalPlaces,
            NegativePattern = negativePattern.Trim(),
        });
    }

    /// <summary>
    /// Replaces all formatting values for this currency definition.
    /// </summary>
    public Result Update(
        string currencyName,
        string symbol,
        CurrencySymbolPosition symbolPosition,
        bool spaceBetweenSymbolAndAmount,
        char decimalSeparator,
        char thousandsSeparator,
        int decimalPlaces,
        string negativePattern)
    {
        if (string.IsNullOrWhiteSpace(currencyName))
            return Result<CurrencyFormat>.Failure(I18nErrors.Validation("CurrencyFormat.CurrencyName.Empty", "Currency name must not be empty."));

        if (string.IsNullOrWhiteSpace(symbol))
            return Result<CurrencyFormat>.Failure(I18nErrors.Validation("CurrencyFormat.Symbol.Empty", "Currency symbol must not be empty."));

        if (decimalSeparator == thousandsSeparator)
            return Result<CurrencyFormat>.Failure(I18nErrors.Validation("CurrencyFormat.Separators.Conflict", "DecimalSeparator and ThousandsSeparator must be different characters."));

        if (decimalPlaces is < 0 or > 10)
            return Result<CurrencyFormat>.Failure(I18nErrors.Validation("CurrencyFormat.DecimalPlaces.OutOfRange", $"DecimalPlaces must be between 0 and 10. Got: {decimalPlaces}."));

        if (string.IsNullOrWhiteSpace(negativePattern) || !negativePattern.Contains("{amount}"))
            return Result<CurrencyFormat>.Failure(I18nErrors.Validation("CurrencyFormat.NegativePattern.Invalid", "NegativePattern must contain the {amount} placeholder."));

        CurrencyName = currencyName.Trim();
        Symbol = symbol.Trim();
        SymbolPosition = symbolPosition;
        SpaceBetweenSymbolAndAmount = spaceBetweenSymbolAndAmount;
        DecimalSeparator = decimalSeparator;
        ThousandsSeparator = thousandsSeparator;
        DecimalPlaces = decimalPlaces;
        NegativePattern = negativePattern.Trim();

        return Result.Success();
    }

    /// <summary>Formats an amount according to this currency's rules.</summary>
    public string Format(decimal amount)
    {
        var isNegative = amount < 0;
        var absStr = FormatAbsoluteAmount(Math.Abs(amount));
        var space = SpaceBetweenSymbolAndAmount ? " " : string.Empty;
        var withSymbol = SymbolPosition == CurrencySymbolPosition.Before ? $"{Symbol}{space}{absStr}" : $"{absStr}{space}{Symbol}";

        if (!isNegative)
            return withSymbol;

        return NegativePattern
            .Replace("{symbol}", SymbolPosition == CurrencySymbolPosition.Before ? $"{Symbol}{space}" : $"{space}{Symbol}")
            .Replace("{amount}", absStr);
    }

    private string FormatAbsoluteAmount(decimal value)
    {
        var rounded = Math.Round(value, DecimalPlaces, MidpointRounding.AwayFromZero);
        var intPart = (long)Math.Truncate(rounded);
        var decPart = rounded - Math.Truncate(rounded);
        var intStr = FormatIntegerWithGrouping(intPart);

        if (DecimalPlaces == 0)
            return intStr;

        var decStr = Math.Abs(decPart).ToString($"F{DecimalPlaces}")[2..];
        return $"{intStr}{DecimalSeparator}{decStr}";
    }

    private string FormatIntegerWithGrouping(long value)
    {
        var str = Math.Abs(value).ToString();
        if (str.Length <= 3)
            return str;

        var parts = new List<string>();
        while (str.Length > 3)
        {
            parts.Insert(0, str[^3..]);
            str = str[..^3];
        }
        if (str.Length > 0)
            parts.Insert(0, str);

        return string.Join(ThousandsSeparator, parts);
    }
}
