using KUKULCAN.SharedKernel.i18n.Application.Features.Currencies.Commands.UpsertCurrencyFormat;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests.Features.Currencies;

[TestFixture]
public sealed class UpsertCurrencyFormatCommandValidatorTests
{
    private readonly UpsertCurrencyFormatCommandValidator _validator = new();

    [Test]
    public void Validate_ValidCommand_ReturnsSuccess()
    {
        var result = _validator.Validate(CreateValidCommand());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("e")]
    [TestCase("english")]
    public void Validate_InvalidLanguageCode_ReturnsFailure(string? languageCode)
    {
        var result = _validator.Validate(CreateValidCommand() with { LanguageCode = languageCode! });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(UpsertCurrencyFormatCommand.LanguageCode)), Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("US")]
    [TestCase("US1")]
    [TestCase("US$")]
    [TestCase("EURO")]
    public void Validate_InvalidCurrencyCode_ReturnsFailure(string? currencyCode)
    {
        var result = _validator.Validate(CreateValidCommand() with { CurrencyCode = currencyCode! });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(UpsertCurrencyFormatCommand.CurrencyCode)), Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Validate_EmptyCurrencyName_ReturnsFailure(string? currencyName)
    {
        var result = _validator.Validate(CreateValidCommand() with { CurrencyName = currencyName! });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(UpsertCurrencyFormatCommand.CurrencyName)), Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Validate_EmptySymbol_ReturnsFailure(string? symbol)
    {
        var result = _validator.Validate(CreateValidCommand() with { Symbol = symbol! });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(UpsertCurrencyFormatCommand.Symbol)), Is.True);
    }

    [TestCase("Before")]
    [TestCase("before")]
    [TestCase("After")]
    [TestCase("after")]
    public void Validate_ValidSymbolPosition_ReturnsSuccess(string symbolPosition)
    {
        var result = _validator.Validate(CreateValidCommand() with { SymbolPosition = symbolPosition });

        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(UpsertCurrencyFormatCommand.SymbolPosition)), Is.False);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("Middle")]
    [TestCase("Left")]
    public void Validate_InvalidSymbolPosition_ReturnsFailure(string? symbolPosition)
    {
        var result = _validator.Validate(CreateValidCommand() with { SymbolPosition = symbolPosition! });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(UpsertCurrencyFormatCommand.SymbolPosition)), Is.True);
    }

    [TestCase(-1)]
    [TestCase(11)]
    public void Validate_DecimalPlacesOutsideRange_ReturnsFailure(int decimalPlaces)
    {
        var result = _validator.Validate(CreateValidCommand() with { DecimalPlaces = decimalPlaces });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(UpsertCurrencyFormatCommand.DecimalPlaces)), Is.True);
    }

    [TestCase(0)]
    [TestCase(2)]
    [TestCase(10)]
    public void Validate_DecimalPlacesWithinRange_ReturnsSuccess(int decimalPlaces)
    {
        var result = _validator.Validate(CreateValidCommand() with { DecimalPlaces = decimalPlaces });

        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(UpsertCurrencyFormatCommand.DecimalPlaces)), Is.False);
    }

    [Test]
    public void Validate_EqualSeparators_ReturnsFailure()
    {
        var result = _validator.Validate(CreateValidCommand() with
        {
            DecimalSeparator = ".",
            ThousandsSeparator = "."
        });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == string.Empty), Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("-")]
    [TestCase("{symbol}")]
    public void Validate_InvalidNegativePattern_ReturnsFailure(string? negativePattern)
    {
        var result = _validator.Validate(CreateValidCommand() with { NegativePattern = negativePattern! });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(UpsertCurrencyFormatCommand.NegativePattern)), Is.True);
    }

    [Test]
    public void Command_DefaultNegativePattern_IsApplied()
    {
        var command = new UpsertCurrencyFormatCommand(
            "es-ES", "EUR", "Euro", "€", "Before", true, ".", ",", 2);

        Assert.That(command.NegativePattern, Is.EqualTo("-{symbol}{amount}"));
    }

    [Test]
    public void Validate_CurrencyNameLongerThanMaximum_ReturnsFailure()
    {
        var result = _validator.Validate(CreateValidCommand() with { CurrencyName = new string('A', 101) });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(UpsertCurrencyFormatCommand.CurrencyName)), Is.True);
    }

    [Test]
    public void Validate_SymbolLongerThanMaximum_ReturnsFailure()
    {
        var result = _validator.Validate(CreateValidCommand() with { Symbol = "123456" });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(UpsertCurrencyFormatCommand.Symbol)), Is.True);
    }

    private static UpsertCurrencyFormatCommand CreateValidCommand() => new(
        "es-ES",
        "EUR",
        "Euro",
        "€",
        "Before",
        true,
        ".",
        ",",
        2,
        "-{symbol}{amount}");
}
