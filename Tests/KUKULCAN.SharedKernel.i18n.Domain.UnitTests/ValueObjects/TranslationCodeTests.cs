using KUKULCAN.SharedKernel.i18n.Domain.ValueObjects;
using KUKULCAN.SharedKernel.Results;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.i18n.Domain.UnitTests.ValueObjects;

[TestFixture]
public sealed class TranslationCodeTests
{
    [TestCase("CRM0001", "CRM", 1)]
    [TestCase("crm0001", "CRM", 1)]
    [TestCase("CORE9999", "CORE", 9999)]
    [TestCase("AB0001", "AB", 1)]
    [TestCase("ABCDE0042", "ABCDE", 42)]
    public void From_ValidCode_ReturnsNormalisedValue(string raw, string module, int sequence)
    {
        Result<TranslationCode> result = TranslationCode.From(raw);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Value, Is.EqualTo($"{module}{sequence:D4}"));
        Assert.That(result.Value.Module, Is.EqualTo(module));
        Assert.That(result.Value.Sequence, Is.EqualTo(sequence));
        Assert.That(result.Value.ToString(), Is.EqualTo(result.Value.Value));
        Assert.That((string)result.Value, Is.EqualTo(result.Value.Value));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void From_EmptyCode_ReturnsFailure(string? raw)
    {
        Assert.That(TranslationCode.From(raw).IsFailure, Is.True);
    }

    [TestCase("A0001")]
    [TestCase("12345678")]
    public void From_TooShortOrInvalidModule_ReturnsFailure(string raw)
    {
        Assert.That(TranslationCode.From(raw).IsFailure, Is.True);
    }

    [TestCase("ABC0000")]
    [TestCase("ABC9999X")]
    [TestCase("ABC12A4")]
    [TestCase("ABC-001")]
    public void From_InvalidNumericPartOrSequence_ReturnsFailure(string raw)
    {
        Assert.That(TranslationCode.From(raw).IsFailure, Is.True);
    }

    [TestCase("ABCDEF0001")]
    [TestCase("A0001")]
    public void From_InvalidModuleLength_ReturnsFailure(string raw)
    {
        Assert.That(TranslationCode.From(raw).IsFailure, Is.True);
    }

    [TestCase("A10001")]
    [TestCase("A1C001")]
    public void From_InvalidModuleCharacters_ReturnsFailure(string raw)
    {
        Assert.That(TranslationCode.From(raw).IsFailure, Is.True);
    }

    [TestCase("crm", 1, "CRM0001")]
    [TestCase(" CRM ", 42, "CRM0042")]
    [TestCase("ABCDE", 9999, "ABCDE9999")]
    public void Create_ValidComponents_ReturnsExpectedCode(string module, int sequence, string expected)
    {
        Result<TranslationCode> result = TranslationCode.Create(module, sequence);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Value, Is.EqualTo(expected));
    }

    [TestCase(null, 1)]
    [TestCase("", 1)]
    [TestCase("A", 1)]
    [TestCase("ABCDEF", 1)]
    [TestCase("AB-", 1)]
    [TestCase("AB", 0)]
    [TestCase("AB", -1)]
    [TestCase("AB", 10000)]
    public void Create_InvalidComponents_ReturnsFailure(string? module, int sequence)
    {
        Assert.That(TranslationCode.Create(module!, sequence).IsFailure, Is.True);
    }

    [Test]
    public void Equality_IsBasedOnValue()
    {
        TranslationCode first = TranslationCode.From("crm0001").Value;
        TranslationCode second = TranslationCode.Create("CRM", 1).Value;

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
    }
}
