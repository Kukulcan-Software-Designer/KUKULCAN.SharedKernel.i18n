using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.Results;

namespace KUKULCAN.SharedKernel.i18n.Domain.UnitTests.Entities;

[TestFixture]
public sealed class LocaleConfigurationTests
{
    private static LocaleConfiguration CreateDefault()
    {
        Result<LocaleConfiguration> result = LocaleConfiguration.Create(
            Guid.NewGuid(), "es-ES", "dd/MM/yyyy", "d/M/yy", "HH:mm", "dd/MM/yyyy HH:mm",
            default, ',', '.', 2, 2);

        Assert.That(result.IsSuccess, Is.True, result.IsFailure ? result.Error.ToString() : string.Empty);
        return result.Value;
    }

    [Test]
    public void Create_ValidInput_NormalisesAndStoresValues()
    {
        LocaleConfiguration config = CreateDefault();

        Assert.Multiple(() =>
        {
            Assert.That(config.LanguageCode.Value, Is.EqualTo("es-ES"));
            Assert.That(config.DateFormat, Is.EqualTo("dd/MM/yyyy"));
            Assert.That(config.ShortDateFormat, Is.EqualTo("d/M/yy"));
            Assert.That(config.TimeFormat, Is.EqualTo("HH:mm"));
            Assert.That(config.DateTimeFormat, Is.EqualTo("dd/MM/yyyy HH:mm"));
            Assert.That(config.DecimalSeparator, Is.EqualTo(','));
            Assert.That(config.ThousandsSeparator, Is.EqualTo('.'));
            Assert.That(config.DecimalPlaces, Is.EqualTo(2));
            Assert.That(config.CurrencyDecimalPlaces, Is.EqualTo(2));
        });
    }

    [Test]
    public void Create_DefaultGuid_ReturnsFailure()
    {
        Result<LocaleConfiguration> result = LocaleConfiguration.Create(
            Guid.Empty, "es-ES", "dd/MM/yyyy", "d/M/yy", "HH:mm", "dd/MM/yyyy HH:mm",
            default, ',', '.', 2, 2);

        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Create_EmptyDateFormat_ReturnsFailure(string? value)
    {
        Result<LocaleConfiguration> result = LocaleConfiguration.Create(Guid.NewGuid(), "es-ES", value!, "d/M/yy", "HH:mm", "dd/MM/yyyy HH:mm", default, ',', '.', 2, 2);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Create_EmptyShortDateFormat_ReturnsFailure()
    {
        Result<LocaleConfiguration> result = LocaleConfiguration.Create(Guid.NewGuid(), "es-ES", "dd/MM/yyyy", "", "HH:mm", "dd/MM/yyyy HH:mm", default, ',', '.', 2, 2);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Create_EmptyTimeFormat_ReturnsFailure()
    {
        Result<LocaleConfiguration> result = LocaleConfiguration.Create(Guid.NewGuid(), "es-ES", "dd/MM/yyyy", "d/M/yy", "", "dd/MM/yyyy HH:mm", default, ',', '.', 2, 2);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Create_EmptyDateTimeFormat_ReturnsFailure()
    {
        Result<LocaleConfiguration> result = LocaleConfiguration.Create(Guid.NewGuid(), "es-ES", "dd/MM/yyyy", "d/M/yy", "HH:mm", "", default, ',', '.', 2, 2);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Create_EqualSeparators_ReturnsFailure()
    {
        Result<LocaleConfiguration> result = LocaleConfiguration.Create(Guid.NewGuid(), "es-ES", "dd/MM/yyyy", "d/M/yy", "HH:mm", "dd/MM/yyyy HH:mm", default, ',', ',', 2, 2);

        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase(-1)]
    [TestCase(11)]
    public void Create_InvalidDecimalPlaces_ReturnsFailure(int decimalPlaces)
    {
        Result<LocaleConfiguration> result = LocaleConfiguration.Create(Guid.NewGuid(), "es-ES", "dd/MM/yyyy", "d/M/yy", "HH:mm", "dd/MM/yyyy HH:mm", default, ',', '.', decimalPlaces, 2);

        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase(-1)]
    [TestCase(11)]
    public void Create_InvalidCurrencyDecimalPlaces_ReturnsFailure(int decimalPlaces)
    {
        Result<LocaleConfiguration> result = LocaleConfiguration.Create(Guid.NewGuid(), "es-ES", "dd/MM/yyyy", "d/M/yy", "HH:mm", "dd/MM/yyyy HH:mm", default, ',', '.', 2, decimalPlaces);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Update_ValidInput_ReplacesAllValues()
    {
        LocaleConfiguration config = CreateDefault();

        Result result = config.Update("MM/dd/yyyy", "M/d/yy", "h:mm tt", "MM/dd/yyyy h:mm tt", default, '.', ',', 3, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(config.DateFormat, Is.EqualTo("MM/dd/yyyy"));
            Assert.That(config.ShortDateFormat, Is.EqualTo("M/d/yy"));
            Assert.That(config.TimeFormat, Is.EqualTo("h:mm tt"));
            Assert.That(config.DateTimeFormat, Is.EqualTo("MM/dd/yyyy h:mm tt"));
            Assert.That(config.DecimalSeparator, Is.EqualTo('.'));
            Assert.That(config.ThousandsSeparator, Is.EqualTo(','));
            Assert.That(config.DecimalPlaces, Is.EqualTo(3));
            Assert.That(config.CurrencyDecimalPlaces, Is.EqualTo(0));
        });
    }

    [Test]
    public void Update_InvalidValues_DoNotMutateExistingState()
    {
        LocaleConfiguration config = CreateDefault();

        Result result = config.Update("changed", "changed", default, "changed", default, '.', '.', 3, 3);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(config.DateFormat, Is.EqualTo("dd/MM/yyyy"));
        Assert.That(config.DecimalSeparator, Is.EqualTo(','));
        Assert.That(config.ThousandsSeparator, Is.EqualTo('.'));
        Assert.That(config.DecimalPlaces, Is.EqualTo(2));
    }
}
