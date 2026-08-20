using System.Globalization;
using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Domain.ValueObjects.Enums;
using KUKULCAN.SharedKernel.Results;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.i18n.Domain.UnitTests.Entities;

[TestFixture]
public sealed class CurrencyFormatTests
{
    private static CurrencyFormat CreateUsdBefore(int decimalPlaces = 2, string negativePattern = "-{symbol}{amount}")
    {
        Result<CurrencyFormat> result = CurrencyFormat.Create(
            Guid.NewGuid(), "en-US", "usd", " US Dollar ", " $ ",
            default, false, '.', ',', decimalPlaces, negativePattern);

        Assert.That(result.IsSuccess, Is.True, result.IsFailure ? result.Error.ToString() : string.Empty);
        return result.Value;
    }

    private static CurrencyFormat CreateEurAfter()
    {
        Result<CurrencyFormat> result = CurrencyFormat.Create(
            Guid.NewGuid(), "es-ES", "eur", " Euro ", " € ",
            CurrencySymbolPosition.After, true, ',', '.', 2, "-{amount} {symbol}");

        Assert.That(result.IsSuccess, Is.True, result.IsFailure ? result.Error.ToString() : string.Empty);
        return result.Value;
    }

    [Test]
    public void Create_ValidInput_NormalisesAndStoresValues()
    {
        CurrencyFormat format = CreateUsdBefore();

        Assert.Multiple(() =>
        {
            Assert.That(format.CurrencyCode, Is.EqualTo("USD"));
            Assert.That(format.CurrencyName, Is.EqualTo("US Dollar"));
            Assert.That(format.Symbol, Is.EqualTo("$"));
            Assert.That(format.LanguageCode.Value, Is.EqualTo("en-US"));
            Assert.That(format.DecimalSeparator, Is.EqualTo('.'));
            Assert.That(format.ThousandsSeparator, Is.EqualTo(','));
            Assert.That(format.DecimalPlaces, Is.EqualTo(2));
        });
    }

    [TestCase("")]
    [TestCase("US")]
    [TestCase("US1")]
    [TestCase("US$")]
    public void Create_InvalidCurrencyCode_ReturnsFailure(string currencyCode)
    {
        Result<CurrencyFormat> result = CurrencyFormat.Create(Guid.NewGuid(), "en-US", currencyCode, "Dollar", "$", default, false, '.', ',', 2);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Create_DefaultGuid_ReturnsFailure()
    {
        Result<CurrencyFormat> result = CurrencyFormat.Create(Guid.Empty, "en-US", "USD", "Dollar", "$", default, false, '.', ',', 2);

        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Create_EmptyCurrencyName_ReturnsFailure(string? currencyName)
    {
        Result<CurrencyFormat> result = CurrencyFormat.Create(Guid.NewGuid(), "en-US", "USD", currencyName!, "$", default, false, '.', ',', 2);

        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Create_EmptySymbol_ReturnsFailure(string? symbol)
    {
        Result<CurrencyFormat> result = CurrencyFormat.Create(Guid.NewGuid(), "en-US", "USD", "Dollar", symbol!, default, false, '.', ',', 2);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Create_EqualSeparators_ReturnsFailure()
    {
        Result<CurrencyFormat> result = CurrencyFormat.Create(Guid.NewGuid(), "en-US", "USD", "Dollar", "$", default, false, '.', '.', 2);

        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase(-1)]
    [TestCase(11)]
    public void Create_DecimalPlacesOutsideRange_ReturnsFailure(int decimalPlaces)
    {
        Result<CurrencyFormat> result = CurrencyFormat.Create(Guid.NewGuid(), "en-US", "USD", "Dollar", "$", default, false, '.', ',', decimalPlaces);

        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("-")]
    [TestCase("{symbol}")]
    public void Create_InvalidNegativePattern_ReturnsFailure(string? pattern)
    {
        Result<CurrencyFormat> result = CurrencyFormat.Create(Guid.NewGuid(), "en-US", "USD", "Dollar", "$", default, false, '.', ',', 2, pattern!);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Format_BeforeSymbol_UsesGroupingAndDecimals()
    {
        CurrencyFormat format = CreateUsdBefore();

        Assert.That(format.Format(1234567.89m), Is.EqualTo("$1,234,567.89"));
        Assert.That(format.Format(12m), Is.EqualTo("$12.00"));
        Assert.That(format.Format(0m), Is.EqualTo("$0.00"));
    }

    [Test]
    public void Format_AfterSymbolWithSpace_UsesLocaleStyle()
    {
        CurrencyFormat format = CreateEurAfter();

        Assert.That(format.Format(1234567.89m), Is.EqualTo("1.234.567,89 €"));
    }

    [Test]
    public void Format_NegativeAmount_UsesConfiguredPattern()
    {
        CurrencyFormat format = CreateUsdBefore(2, "-{symbol}{amount}");

        Assert.That(format.Format(-1234.56m), Is.EqualTo("-$1,234.56"));
    }

    [Test]
    public void Format_AccountingNegativePattern_UsesParentheses()
    {
        CurrencyFormat format = CreateUsdBefore(2, "({symbol}{amount})");

        Assert.That(format.Format(-1234.56m), Is.EqualTo("($1,234.56)"));
    }

    [TestCase("0.004", "$0.00")]
    [TestCase("0.005", "$0.01")]
    [TestCase("1.234", "$1.23")]
    [TestCase("1.235", "$1.24")]
    public void Format_RoundsToConfiguredDecimalPlaces(string amountText, string expected)
    {
        decimal amount = decimal.Parse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture);
        CurrencyFormat format = CreateUsdBefore();

        Assert.That(format.Format(amount), Is.EqualTo(expected));
    }

    [Test]
    public void Format_ZeroDecimalPlaces_DoesNotEmitDecimalSeparator()
    {
        CurrencyFormat format = CreateUsdBefore(0);

        Assert.That(format.Format(1234.56m), Is.EqualTo("$1,235"));
    }

    [Test]
    public void Update_ValidInput_ReplacesFormattingProperties()
    {
        CurrencyFormat format = CreateUsdBefore();

        Result result = format.Update("US Dollar Updated", "USD", CurrencySymbolPosition.After, true, ',', '.', 3, "-{amount} {symbol}");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(format.CurrencyName, Is.EqualTo("US Dollar Updated"));
            Assert.That(format.Symbol, Is.EqualTo("USD"));
            Assert.That(format.SymbolPosition, Is.EqualTo(CurrencySymbolPosition.After));
            Assert.That(format.SpaceBetweenSymbolAndAmount, Is.True);
            Assert.That(format.DecimalSeparator, Is.EqualTo(','));
            Assert.That(format.ThousandsSeparator, Is.EqualTo('.'));
            Assert.That(format.DecimalPlaces, Is.EqualTo(3));
            Assert.That(format.NegativePattern, Is.EqualTo("-{amount} {symbol}"));
        });
    }

    [Test]
    public void Update_InvalidValues_DoNotMutateExistingState()
    {
        CurrencyFormat format = CreateUsdBefore();

        Result result = format.Update("Changed", "€", CurrencySymbolPosition.Before, false, '.', '.', 2, "-{symbol}{amount}");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(format.CurrencyName, Is.EqualTo("US Dollar"));
        Assert.That(format.Symbol, Is.EqualTo("$"));
        Assert.That(format.DecimalSeparator, Is.EqualTo('.'));
        Assert.That(format.ThousandsSeparator, Is.EqualTo(','));
    }
}
