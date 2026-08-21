using FluentValidation.Results;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Commands.BulkUpsertTranslations;
using KUKULCAN.SharedKernel.i18n.Domain.DTOs;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests.Features.Translations;

[TestFixture]
public sealed class BulkUpsertTranslationsCommandValidatorTests
{
    private readonly BulkUpsertTranslationsCommandValidator _validator = new();

    [Test]
    public void Validate_ValidItem_ReturnsSuccess()
    {
        ValidationResult result = _validator.Validate(new BulkUpsertTranslationsCommand(
            [new TestBulkTranslationDto("CRM0001", "es-ES", "Hola")]));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_EmptyItems_ReturnsFailure()
    {
        ValidationResult result = _validator.Validate(new BulkUpsertTranslationsCommand([]));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyCode_ReturnsFailure()
    {
        ValidationResult result = _validator.Validate(new BulkUpsertTranslationsCommand(
            [new TestBulkTranslationDto("", "es-ES", "Hola")]));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyLanguageCode_ReturnsFailure()
    {
        ValidationResult result = _validator.Validate(new BulkUpsertTranslationsCommand(
            [new TestBulkTranslationDto("CRM0001", "", "Hola")]));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyText_ReturnsFailure()
    {
        ValidationResult result = _validator.Validate(new BulkUpsertTranslationsCommand(
            [new TestBulkTranslationDto("CRM0001", "es-ES", "")]));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_TextLongerThanMaximum_ReturnsFailure()
    {
        ValidationResult result = _validator.Validate(new BulkUpsertTranslationsCommand(
            [new TestBulkTranslationDto("CRM0001", "es-ES", new string('A', 4001))]));

        Assert.That(result.IsValid, Is.False);
    }

    private sealed record TestBulkTranslationDto(string Code, string LanguageCode, string Text, string? Context = null,
        int? MaxLength = null) : BulkTranslationDto(Code, LanguageCode, Text, Context, MaxLength);
}
