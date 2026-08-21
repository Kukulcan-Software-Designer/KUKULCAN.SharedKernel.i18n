using KUKULCAN.SharedKernel.i18n.Application.Features.Locales.Commands.UpsertLocaleConfiguration;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests.Features.Locales;

[TestFixture]
public sealed class UpsertLocaleConfigurationCommandValidatorTests
{
    private readonly UpsertLocaleConfigurationCommandValidator _validator = new();

    [Test]
    public void Validate_ValidCommand_ReturnsSuccess()
    {
        var result = _validator.Validate(CreateValidCommand());

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("english")]
    public void Validate_InvalidLanguageCode_ReturnsFailure(string? value)
    {
        var result = _validator.Validate(CreateValidCommand() with { LanguageCode = value! });

        Assert.That(result.IsValid, Is.False);
    }

    [TestCase(null)]
    [TestCase("")]
    public void Validate_EmptyDateFormats_ReturnsFailure(string? value)
    {
        var result = _validator.Validate(CreateValidCommand() with { DateFormat = value!, ShortDateFormat = value!, TimeFormat = value!, DateTimeFormat = value! });

        Assert.That(result.IsValid, Is.False);
    }

    [TestCase("Sunday")]
    [TestCase("Monday")]
    [TestCase("Saturday")]
    public void Validate_ValidFirstDayOfWeek_ReturnsSuccess(string value)
    {
        var result = _validator.Validate(CreateValidCommand() with { FirstDayOfWeek = value });

        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(UpsertLocaleConfigurationCommand.FirstDayOfWeek)), Is.False);
    }

    [TestCase("")]
    [TestCase("Friday")]
    public void Validate_InvalidFirstDayOfWeek_ReturnsFailure(string value)
    {
        var result = _validator.Validate(CreateValidCommand() with { FirstDayOfWeek = value });

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_EqualSeparators_ReturnsFailure()
    {
        var result = _validator.Validate(CreateValidCommand() with { DecimalSeparator = ".", ThousandsSeparator = "." });

        Assert.That(result.IsValid, Is.False);
    }

    [TestCase("")]
    [TestCase("..")] 
    public void Validate_InvalidDecimalSeparator_ReturnsFailure(string value)
    {
        var result = _validator.Validate(CreateValidCommand() with { DecimalSeparator = value });

        Assert.That(result.IsValid, Is.False);
    }

    [TestCase(-1)]
    [TestCase(11)]
    public void Validate_DecimalPlacesOutsideRange_ReturnsFailure(int value)
    {
        var result = _validator.Validate(CreateValidCommand() with { DecimalPlaces = value });

        Assert.That(result.IsValid, Is.False);
    }

    [TestCase(-1)]
    [TestCase(11)]
    public void Validate_CurrencyDecimalPlacesOutsideRange_ReturnsFailure(int value)
    {
        var result = _validator.Validate(CreateValidCommand() with { CurrencyDecimalPlaces = value });

        Assert.That(result.IsValid, Is.False);
    }

    private static UpsertLocaleConfigurationCommand CreateValidCommand() => new(
        "es-ES", "dd/MM/yyyy", "dd/MM/yy", "HH:mm:ss", "dd/MM/yyyy HH:mm:ss",
        "Monday", ",", ".", 2, 2);
}
