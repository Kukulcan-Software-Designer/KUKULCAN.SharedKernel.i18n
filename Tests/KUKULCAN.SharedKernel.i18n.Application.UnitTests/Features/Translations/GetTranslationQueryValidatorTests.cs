using FluentValidation.Results;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Queries.GetTranslation;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests.Features.Translations;

[TestFixture]
public sealed class GetTranslationQueryValidatorTests
{
    private readonly GetTranslationQueryValidator _validator = new();

    [Test]
    public void Validate_ValidQuery_ReturnsSuccess()
    {
        ValidationResult result = _validator.Validate(new GetTranslationQuery("CRM0001", "es-ES"));

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("CRM")]
    public void Validate_InvalidCode_ReturnsFailure(string? code)
    {
        ValidationResult result = _validator.Validate(new GetTranslationQuery(code!, "es-ES"));

        Assert.That(result.IsValid, Is.False);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("spanish")]
    public void Validate_InvalidLanguageCode_ReturnsFailure(string? languageCode)
    {
        ValidationResult result = _validator.Validate(new GetTranslationQuery("CRM0001", languageCode!));

        Assert.That(result.IsValid, Is.False);
    }
}
