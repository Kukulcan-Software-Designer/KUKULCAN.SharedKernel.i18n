using KUKULCAN.SharedKernel.i18n.Domain.ValueObjects;
using KUKULCAN.SharedKernel.Results;

namespace KUKULCAN.SharedKernel.i18n.Domain.UnitTests.ValueObjects;

[TestFixture]
public sealed class LanguageCodeTests
{
    [TestCase("es-ES", "es-ES", "es", "ES")]
    [TestCase("ES-es", "es-ES", "es", "ES")]
    [TestCase("en", "en", "en", null)]
    [TestCase("ca-ES", "ca-ES", "ca", "ES")]
    public void Create_ValidTag_NormalisesAndParses(string input, string value, string language, string? region)
    {
        Result<LanguageCode> result = LanguageCode.Create(input);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Value, Is.EqualTo(value));
        Assert.That(result.Value.Language, Is.EqualTo(language));
        Assert.That(result.Value.Region, Is.EqualTo(region));
        Assert.That(result.Value.ToString(), Is.EqualTo(value));
        Assert.That((string)result.Value, Is.EqualTo(value));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_EmptyTag_ReturnsFailure(string? input)
    {
        Result<LanguageCode> result = LanguageCode.Create(input);

        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase("e")]
    [TestCase("english")]
    [TestCase("es_")]
    [TestCase("es--ES")]
    [TestCase("es-123456789")]
    public void Create_InvalidTag_ReturnsFailure(string input)
    {
        Result<LanguageCode> result = LanguageCode.Create(input);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void FallbackChain_RegionalLanguage_ReturnsExactLanguageThenParentThenEnglish()
    {
        Result<LanguageCode> result = LanguageCode.Create("es-MX");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.FallbackChain, Is.EqualTo(new[] { "es-MX", "es", "en" }));
    }

    [Test]
    public void FallbackChain_LanguageOnly_DoesNotDuplicateEnglish()
    {
        Result<LanguageCode> result = LanguageCode.Create("en");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.FallbackChain, Is.EqualTo(new[] { "en" }));
    }

    [Test]
    public void FallbackChain_IsReadOnlyAndStableAcrossReads()
    {
        Result<LanguageCode> result = LanguageCode.Create("fr-FR");

        IReadOnlyList<string> first = result.Value.FallbackChain;
        IReadOnlyList<string> second = result.Value.FallbackChain;

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first, Is.Not.SameAs(second));
    }

    [TestCase("zh-Hant-TW")]
    [TestCase("sr-Latn-RS")]
    public void Create_ComplexBcp47Tag_IsAccepted(string input)
    {
        Result<LanguageCode> result = LanguageCode.Create(input);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void Equality_IsBasedOnNormalisedValue()
    {
        LanguageCode first = LanguageCode.Create("ES-es").Value;
        LanguageCode second = LanguageCode.Create("es-ES").Value;

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
    }
}
