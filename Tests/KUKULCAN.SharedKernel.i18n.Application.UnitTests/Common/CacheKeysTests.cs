using KUKULCAN.SharedKernel.i18n.Application.Common;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests.Common;

[TestFixture]
public sealed class CacheKeysTests
{
    [Test]
    public void LanguageConstants_HaveExpectedValues()
    {
        Assert.That(I18NCacheKeys.LanguagesAll, Is.EqualTo("i18n:languages:all"));
        Assert.That(I18NCacheKeys.LanguagesActive, Is.EqualTo("i18n:languages:active"));
        Assert.That(I18NCacheKeys.LanguageDefault, Is.EqualTo("i18n:languages:default"));
    }

    [Test]
    public void Language_NormalizesCodeToLowerInvariant()
    {
        Assert.That(I18NCacheKeys.Language("ES-es"), Is.EqualTo("i18n:language:es-es"));
    }

    [Test]
    public void Translation_NormalizesCodeAndLanguage()
    {
        Assert.That(I18NCacheKeys.Translation("crm0001", "ES-ES"), Is.EqualTo("i18n:t:CRM0001:es-es"));
    }

    [Test]
    public void ModuleTranslations_NormalizesModuleAndLanguage()
    {
        Assert.That(I18NCacheKeys.ModuleTranslations("crm", "ES-ES"), Is.EqualTo("i18n:module:CRM:es-es"));
    }

    [Test]
    public void LocaleConfig_NormalizesLanguage()
    {
        Assert.That(I18NCacheKeys.LocaleConfig("ES-ES"), Is.EqualTo("i18n:locale:es-es"));
    }

    [Test]
    public void CurrencyFormats_NormalizesLanguage()
    {
        Assert.That(I18NCacheKeys.CurrencyFormats("ES-ES"), Is.EqualTo("i18n:currencies:es-es"));
    }

    [Test]
    public void CurrencyFormat_NormalizesLanguageAndCurrencyCode()
    {
        Assert.That(I18NCacheKeys.CurrencyFormat("ES-ES", "eur"), Is.EqualTo("i18n:currency:es-es:EUR"));
    }

    [Test]
    public void AllKeys_UseTheI18NPrefix()
    {
        string[] keys =
        [
            I18NCacheKeys.LanguagesAll,
            I18NCacheKeys.LanguagesActive,
            I18NCacheKeys.LanguageDefault,
            I18NCacheKeys.Language("es-ES"),
            I18NCacheKeys.Translation("CRM0001", "es-ES"),
            I18NCacheKeys.ModuleTranslations("CRM", "es-ES"),
            I18NCacheKeys.LocaleConfig("es-ES"),
            I18NCacheKeys.CurrencyFormats("es-ES"),
            I18NCacheKeys.CurrencyFormat("es-ES", "EUR")
        ];

        Assert.That(keys, Is.All.StartsWith("i18n:"));
    }
}
