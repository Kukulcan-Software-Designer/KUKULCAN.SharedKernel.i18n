using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Queries.GetTranslationsByModule;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests.Features.Translations;

[TestFixture]
public sealed class GetTranslationsByModuleQueryValidatorTests
{
    private readonly GetTranslationsByModuleQueryValidator _validator = new();

    [Test]
    public void Validate_ValidQuery_ReturnsSuccess()
    {
        var result = _validator.Validate(new GetTranslationsByModuleQuery("CRM", "es-ES"));

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("C")]
    [TestCase("CRM1")]
    [TestCase("CRM-")]
    public void Validate_InvalidModule_ReturnsFailure(string? module)
    {
        var result = _validator.Validate(new GetTranslationsByModuleQuery(module!, "es-ES"));

        Assert.That(result.IsValid, Is.False);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("spanish")]
    public void Validate_InvalidLanguageCode_ReturnsFailure(string? languageCode)
    {
        var result = _validator.Validate(new GetTranslationsByModuleQuery("CRM", languageCode!));

        Assert.That(result.IsValid, Is.False);
    }
}
