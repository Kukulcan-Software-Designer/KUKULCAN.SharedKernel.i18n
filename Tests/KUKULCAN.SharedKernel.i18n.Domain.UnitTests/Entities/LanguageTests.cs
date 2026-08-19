using KUKULCAN.SharedKernel.i18n.Domain.Entities;

namespace KUKULCAN.SharedKernel.i18n.Domain.UnitTests.Entities;

[TestFixture]
public sealed class LanguageTests
{
    private static Language CreateLanguage(string code = "es-ES", bool isDefault = false)
    {
        var result = Language.Create(Guid.NewGuid(), code, "Spanish", "Español", isDefault);

        Assert.That(result.IsSuccess, Is.True, result.IsFailure ? result.Error.ToString() : string.Empty);
        return result.Value;
    }

    private static CurrencyFormat CreateCurrency(string code = "USD")
    {
        var result = CurrencyFormat.Create(Guid.NewGuid(), "es-ES", code, "Dólar estadounidense", "$", default, false, ',', '.', 2);

        Assert.That(result.IsSuccess, Is.True, result.IsFailure ? result.Error.ToString() : string.Empty);
        return result.Value;
    }

    [Test]
    public void Create_ValidInput_NormalisesAndInitialisesActiveState()
    {
        var id = Guid.NewGuid();
        var result = Language.Create(id, "ES-es", " Spanish ", " Español ", true);

        Assert.That(result.IsSuccess, Is.True);
        var language = result.Value;

        Assert.Multiple(() =>
        {
            Assert.That(language.Id.Value, Is.EqualTo(id));
            Assert.That(language.Code, Is.EqualTo("es-ES"));
            Assert.That(language.Name, Is.EqualTo("Spanish"));
            Assert.That(language.NativeName, Is.EqualTo("Español"));
            Assert.That(language.IsDefault, Is.True);
            Assert.That(language.IsActive, Is.True);
            Assert.That(language.CurrencyFormats, Is.Empty);
            Assert.That(language.LocaleConfiguration, Is.Null);
        });
    }

    [Test]
    public void Create_DefaultGuid_ReturnsFailure()
    {
        var result = Language.Create(Guid.Empty, "es-ES", "Spanish", "Español");

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Create_InvalidLanguageCode_ReturnsFailure()
    {
        var result = Language.Create(Guid.NewGuid(), "invalid", "Spanish", "Español");

        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Create_EmptyName_ReturnsFailure(string? name)
    {
        var result = Language.Create(Guid.NewGuid(), "es-ES", name!, "Español");

        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Create_EmptyNativeName_ReturnsFailure(string? nativeName)
    {
        var result = Language.Create(Guid.NewGuid(), "es-ES", "Spanish", nativeName!);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Update_ValidNames_TrimsAndUpdates()
    {
        var language = CreateLanguage();

        var result = language.Update(" Castilian Spanish ", " Castellano ");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(language.Name, Is.EqualTo("Castilian Spanish"));
            Assert.That(language.NativeName, Is.EqualTo("Castellano"));
        });
    }

    [Test]
    public void Update_InvalidName_DoesNotMutateState()
    {
        var language = CreateLanguage();

        var result = language.Update(" ", "Castellano");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(language.Name, Is.EqualTo("Spanish"));
        Assert.That(language.NativeName, Is.EqualTo("Español"));
    }

    [Test]
    public void Deactivate_NonDefaultLanguage_Deactivates()
    {
        var language = CreateLanguage(isDefault: false);

        var result = language.Deactivate();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(language.IsActive, Is.False);
    }

    [Test]
    public void Deactivate_DefaultLanguage_ReturnsConflictAndRemainsActive()
    {
        var language = CreateLanguage(isDefault: true);

        var result = language.Deactivate();

        Assert.That(result.IsFailure, Is.True);
        Assert.That(language.IsActive, Is.True);
        Assert.That(language.IsDefault, Is.True);
    }

    [Test]
    public void Activate_ReactivatesInactiveLanguage()
    {
        var language = CreateLanguage();
        Assert.That(language.Deactivate().IsSuccess, Is.True);

        var result = language.Activate();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(language.IsActive, Is.True);
    }

    [Test]
    public void SetLocaleConfiguration_AttachesConfiguration()
    {
        var language = CreateLanguage();
        var configResult = LocaleConfiguration.Create(Guid.NewGuid(), "es-ES", "dd/MM/yyyy", "d/M/yy", "HH:mm", "dd/MM/yyyy HH:mm", default, ',', '.', 2, 2);
        Assert.That(configResult.IsSuccess, Is.True);

        language.SetLocaleConfiguration(configResult.Value);

        Assert.That(language.LocaleConfiguration, Is.SameAs(configResult.Value));
    }

    [Test]
    public void SetLocaleConfiguration_Null_Throws()
    {
        var language = CreateLanguage();

        Assert.Throws<ArgumentNullException>(() => language.SetLocaleConfiguration(null!));
    }

    [Test]
    public void AddCurrencyFormat_AddsFirstFormat()
    {
        var language = CreateLanguage();
        var format = CreateCurrency();

        var result = language.AddCurrencyFormat(format);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(language.CurrencyFormats, Has.Count.EqualTo(1));
        Assert.That(language.CurrencyFormats[0], Is.SameAs(format));
    }

    [Test]
    public void AddCurrencyFormat_DuplicateCurrency_ReturnsConflictAndDoesNotAdd()
    {
        var language = CreateLanguage();
        Assert.That(language.AddCurrencyFormat(CreateCurrency("USD")).IsSuccess, Is.True);
        var duplicate = CreateCurrency("usd");

        var result = language.AddCurrencyFormat(duplicate);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(language.CurrencyFormats, Has.Count.EqualTo(1));
    }

    [Test]
    public void AddCurrencyFormat_Null_Throws()
    {
        var language = CreateLanguage();

        Assert.Throws<ArgumentNullException>(() => language.AddCurrencyFormat(null!));
    }

    [Test]
    public void RemoveCurrencyFormat_IsCaseInsensitive()
    {
        var language = CreateLanguage();
        Assert.That(language.AddCurrencyFormat(CreateCurrency("USD")).IsSuccess, Is.True);

        var result = language.RemoveCurrencyFormat("usd");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(language.CurrencyFormats, Is.Empty);
    }

    [Test]
    public void RemoveCurrencyFormat_MissingCurrency_ReturnsNotFound()
    {
        var language = CreateLanguage();

        var result = language.RemoveCurrencyFormat("EUR");

        Assert.That(result.IsFailure, Is.True);
    }
}
