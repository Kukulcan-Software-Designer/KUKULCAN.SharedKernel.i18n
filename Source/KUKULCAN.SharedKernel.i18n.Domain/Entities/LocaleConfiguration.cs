namespace KUKULCAN.SharedKernel.i18n.Domain.Entities;

/// <summary>
/// Stores locale-specific formatting rules for a given language:
/// date and time patterns, number separators, and decimal precision.
///
/// <para>
/// This is a <b>global</b> entity (not tenant-scoped) owned within the
/// <see cref="Language"/> aggregate. One <see cref="LocaleConfiguration"/>
/// exists per language.
/// </para>
/// </summary>
/// <example>
/// <code>
/// var cfg = LocaleConfiguration.Create(
///     id:                   Guid.CreateVersion7(),
///     languageCode:         "es-ES",
///     dateFormat:           "dd/MM/yyyy",
///     shortDateFormat:      "d/M/yy",
///     timeFormat:           "HH:mm",
///     dateTimeFormat:       "dd/MM/yyyy HH:mm",
///     firstDayOfWeek:       FirstDayOfWeek.Monday,
///     decimalSeparator:     ',',
///     thousandsSeparator:   '.',
///     decimalPlaces:        2,
///     currencyDecimalPlaces: 2).Value;
/// </code>
/// </example>
public sealed class LocaleConfiguration : AuditableEntity<I18nEntityId>
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>
    /// BCP-47 code of the language this configuration belongs to.
    /// </summary>
    public LanguageCode LanguageCode { get; private set; } = null!;

    // ── Date / Time ───────────────────────────────────────────────────────────

    /// <summary>
    /// Full date pattern, e.g. <c>"MM/dd/yyyy"</c> (en-US) or <c>"dd/MM/yyyy"</c> (es-ES).
    /// </summary>
    public string DateFormat { get; private set; } = string.Empty;

    /// <summary>Short date pattern, e.g. <c>"M/d/yy"</c>.</summary>
    public string ShortDateFormat { get; private set; } = string.Empty;

    /// <summary>
    /// Time pattern, e.g. <c>"h:mm tt"</c> (12-hour) or <c>"HH:mm"</c> (24-hour).
    /// </summary>
    public string TimeFormat { get; private set; } = string.Empty;

    /// <summary>
    /// Combined date-time pattern, e.g. <c>"MM/dd/yyyy h:mm tt"</c>.
    /// </summary>
    public string DateTimeFormat { get; private set; } = string.Empty;

    /// <summary>
    /// First day of the calendar week for this locale.
    /// </summary>
    public FirstDayOfWeek FirstDayOfWeek { get; private set; }

    // ── Number formatting ─────────────────────────────────────────────────────

    /// <summary>
    /// Decimal separator char: <c>'.'</c> (en-US) or <c>','</c> (es-ES, de-DE).
    /// </summary>
    public char DecimalSeparator { get; private set; }

    /// <summary>
    /// Thousands grouping separator: <c>','</c> (en-US), <c>'.'</c> (es-ES), <c>' '</c> (fr-FR).
    /// </summary>
    public char ThousandsSeparator { get; private set; }

    /// <summary>
    /// Default decimal places for non-monetary numbers (typically 2).
    /// </summary>
    public int DecimalPlaces { get; private set; }

    /// <summary>
    /// Default decimal places for monetary amounts.
    /// Most currencies: 2. JPY/KRW: 0. KWD/BHD: 3.
    /// Individual <see cref="CurrencyFormat"/> entries override this per-currency.
    /// </summary>
    public int CurrencyDecimalPlaces { get; private set; }

    // ── EF Core constructor ───────────────────────────────────────────────────

    // ReSharper disable once UnusedMember.Local
    private LocaleConfiguration() { }

    // ── Factory method ────────────────────────────────────────────────────────
    /// <summary>
    /// Creates a new <see cref="LocaleConfiguration"/> with all formatting rules.
    /// </summary>
    /// <param name="id">The id parameter.</param>
    /// <param name="languageCode">The languageCode parameter.</param>
    /// <param name="dateFormat">The dateFormat parameter.</param>
    /// <param name="shortDateFormat">The shortDateFormat parameter.</param>
    /// <param name="timeFormat">The timeFormat parameter.</param>
    /// <param name="dateTimeFormat">The dateTimeFormat parameter.</param>
    /// <param name="firstDayOfWeek">The firstDayOfWeek parameter.</param>
    /// <param name="decimalSeparator">The decimalSeparator parameter.</param>
    /// <param name="thousandsSeparator">The thousandsSeparator parameter.</param>
    /// <param name="decimalPlaces">The decimalPlaces parameter.</param>
    /// <param name="currencyDecimalPlaces">The currencyDecimalPlaces parameter.</param>
    /// <returns>The operation result.</returns>
    public static Result<LocaleConfiguration> Create(Guid id, string languageCode, string dateFormat, string shortDateFormat, string timeFormat, string dateTimeFormat,
        FirstDayOfWeek firstDayOfWeek, char decimalSeparator, char thousandsSeparator, int decimalPlaces = 2, int currencyDecimalPlaces = 2)
    {
        var langResult = LanguageCode.Create(languageCode);
        if (langResult.IsFailure)
            return Result<LocaleConfiguration>.Failure(langResult.Error);

        if (string.IsNullOrWhiteSpace(dateFormat))
            return Result<LocaleConfiguration>.Failure(I18nErrors.Validation("LocaleConfig.DateFormat.Empty", "DateFormat must not be empty."));

        if (string.IsNullOrWhiteSpace(shortDateFormat))
            return Result<LocaleConfiguration>.Failure(I18nErrors.Validation("LocaleConfig.ShortDateFormat.Empty", "ShortDateFormat must not be empty."));

        if (string.IsNullOrWhiteSpace(timeFormat))
            return Result<LocaleConfiguration>.Failure(I18nErrors.Validation("LocaleConfig.TimeFormat.Empty", "TimeFormat must not be empty."));

        if (string.IsNullOrWhiteSpace(dateTimeFormat))
            return Result<LocaleConfiguration>.Failure(I18nErrors.Validation("LocaleConfig.DateTimeFormat.Empty", "DateTimeFormat must not be empty."));

        if (decimalSeparator == thousandsSeparator)
            return Result<LocaleConfiguration>.Failure(I18nErrors.Validation("LocaleConfig.Separators.Conflict", "DecimalSeparator and ThousandsSeparator must be different characters."));

        if (decimalPlaces is < 0 or > 10)
            return Result<LocaleConfiguration>.Failure(I18nErrors.Validation("LocaleConfig.DecimalPlaces.OutOfRange", $"DecimalPlaces must be between 0 and 10. Got: {decimalPlaces}."));

        if (currencyDecimalPlaces is < 0 or > 10)
            return Result<LocaleConfiguration>.Failure(I18nErrors.Validation("LocaleConfig.CurrencyDecimalPlaces.OutOfRange", $"CurrencyDecimalPlaces must be between 0 and 10. Got: {currencyDecimalPlaces}."));

        return Result<LocaleConfiguration>.Success(new LocaleConfiguration
        {
            Id = new I18nEntityId(Guard.NotDefault(id, nameof(id))),
            LanguageCode = langResult.Value,
            DateFormat = dateFormat.Trim(),
            ShortDateFormat = shortDateFormat.Trim(),
            TimeFormat = timeFormat.Trim(),
            DateTimeFormat = dateTimeFormat.Trim(),
            FirstDayOfWeek = firstDayOfWeek,
            DecimalSeparator = decimalSeparator,
            ThousandsSeparator = thousandsSeparator,
            DecimalPlaces = decimalPlaces,
            CurrencyDecimalPlaces = currencyDecimalPlaces,
        });
    }

    // ── Business method ───────────────────────────────────────────────────────
    /// <summary>
    /// Replaces all formatting values for this locale configuration.
    /// </summary>
    /// <param name="dateFormat">The dateFormat parameter.</param>
    /// <param name="shortDateFormat">The shortDateFormat parameter.</param>
    /// <param name="timeFormat">The timeFormat parameter.</param>
    /// <param name="dateTimeFormat">The dateTimeFormat parameter.</param>
    /// <param name="firstDayOfWeek">The firstDayOfWeek parameter.</param>
    /// <param name="decimalSeparator">The decimalSeparator parameter.</param>
    /// <param name="thousandsSeparator">The thousandsSeparator parameter.</param>
    /// <param name="decimalPlaces">The decimalPlaces parameter.</param>
    /// <param name="currencyDecimalPlaces">The currencyDecimalPlaces parameter.</param>
    /// <returns>The operation result.</returns>
    public Result Update(string dateFormat, string shortDateFormat, string timeFormat, string dateTimeFormat, FirstDayOfWeek firstDayOfWeek,
        char decimalSeparator, char thousandsSeparator, int decimalPlaces, int currencyDecimalPlaces)
    {
        if (decimalSeparator == thousandsSeparator)
            return Result<LocaleConfiguration>.Failure(I18nErrors.Validation("LocaleConfig.Separators.Conflict", "DecimalSeparator and ThousandsSeparator must be different characters."));

        if (decimalPlaces is < 0 or > 10)
            return Result<LocaleConfiguration>.Failure(I18nErrors.Validation("LocaleConfig.DecimalPlaces.OutOfRange", $"DecimalPlaces must be between 0 and 10. Got: {decimalPlaces}."));

        if (currencyDecimalPlaces is < 0 or > 10)
            return Result<LocaleConfiguration>.Failure(I18nErrors.Validation("LocaleConfig.CurrencyDecimalPlaces.OutOfRange", $"CurrencyDecimalPlaces must be between 0 and 10. Got: {currencyDecimalPlaces}."));

        DateFormat = I18nGuard.NullOrWhiteSpace(dateFormat, nameof(dateFormat)).Trim();
        ShortDateFormat = I18nGuard.NullOrWhiteSpace(shortDateFormat, nameof(shortDateFormat)).Trim();
        TimeFormat = I18nGuard.NullOrWhiteSpace(timeFormat, nameof(timeFormat)).Trim();
        DateTimeFormat = I18nGuard.NullOrWhiteSpace(dateTimeFormat, nameof(dateTimeFormat)).Trim();
        FirstDayOfWeek = firstDayOfWeek;
        DecimalSeparator = decimalSeparator;
        ThousandsSeparator = thousandsSeparator;
        DecimalPlaces = decimalPlaces;
        CurrencyDecimalPlaces = currencyDecimalPlaces;
        return Result.Success();
    }
}
