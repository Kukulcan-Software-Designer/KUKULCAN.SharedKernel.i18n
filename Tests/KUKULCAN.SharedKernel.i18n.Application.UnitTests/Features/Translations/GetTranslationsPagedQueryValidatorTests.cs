using FluentValidation.Results;
using KUKULCAN.SharedKernel.i18n.Application.Common.Pagination;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Queries.GetTranslationsPaged;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests.Features.Translations;

[TestFixture]
public sealed class GetTranslationsPagedQueryValidatorTests
{
    private readonly GetTranslationsPagedQueryValidator _validator = new();

    [Test]
    public void Validate_NoFilters_ReturnsSuccess()
    {
        ValidationResult result = _validator.Validate(new GetTranslationsPagedQuery(new PaginationRequest()));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_ValidFilters_ReturnsSuccess()
    {
        ValidationResult result = _validator.Validate(new GetTranslationsPagedQuery(new PaginationRequest(), "CRM", "es-ES"));

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase("")]
    [TestCase("C")]
    [TestCase("CRM1")]
    [TestCase("CRM-")]
    public void Validate_InvalidModuleFilter_ReturnsFailure(string module)
    {
        ValidationResult result = _validator.Validate(new GetTranslationsPagedQuery(new PaginationRequest(), module));

        Assert.That(result.IsValid, Is.False);
    }

    [TestCase("")]
    [TestCase("spanish")]
    public void Validate_InvalidLanguageFilter_ReturnsFailure(string language)
    {
        ValidationResult result = _validator.Validate(new GetTranslationsPagedQuery(new PaginationRequest(), LanguageFilter: language));

        Assert.That(result.IsValid, Is.False);
    }
}
