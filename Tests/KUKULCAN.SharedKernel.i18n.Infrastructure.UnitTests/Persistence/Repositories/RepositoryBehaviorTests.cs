using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Domain.ValueObjects;
using KUKULCAN.SharedKernel.i18n.Domain.ValueObjects.Enums;
using DomainLanguage = KUKULCAN.SharedKernel.i18n.Domain.Entities.Language;
using DomainTranslation = KUKULCAN.SharedKernel.i18n.Domain.Entities.Translation;

[assembly: LevelOfParallelism(1)]

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.UnitTests.Persistence.Repositories;

internal static class RepositoryTestData
{
    public static DomainLanguage Language(string code = "es-ES", string name = "Spanish", bool isDefault = false)
        => DomainLanguage.Create(Guid.CreateVersion7(), code, name, name, isDefault).Value;

    public static LocaleConfiguration Locale(string languageCode = "es-ES")
        => LocaleConfiguration.Create(Guid.CreateVersion7(), languageCode, "dd/MM/yyyy", "dd/MM/yyyy", "HH:mm", "dd/MM/yyyy HH:mm", FirstDayOfWeek.Monday, ',', '.', 2, 2).Value;

    public static CurrencyFormat Currency(string languageCode = "es-ES", string code = "EUR")
        => CurrencyFormat.Create(Guid.CreateVersion7(), languageCode, code, "Euro", "€", CurrencySymbolPosition.After, true, ',', '.', 2, "-{amount} {symbol}").Value;

    public static DomainTranslation Translation(string code = "CRM0001", string languageCode = "es-ES", string text = "Hola")
        => DomainTranslation.Create(Guid.CreateVersion7(), code, languageCode, text).Value;

    public static LanguageCode LanguageCode(string value) => KUKULCAN.SharedKernel.i18n.Domain.ValueObjects.LanguageCode.Create(value).Value;
    public static TranslationCode TranslationCode(string value) => KUKULCAN.SharedKernel.i18n.Domain.ValueObjects.TranslationCode.From(value).Value;
}
