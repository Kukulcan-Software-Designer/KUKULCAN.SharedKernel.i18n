using FluentValidation.Results;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Commands.UpdateTranslation;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests.Features.Translations;

[TestFixture]
public sealed class UpdateTranslationCommandValidatorTests
{
    private readonly UpdateTranslationCommandValidator _validator = new();

    [Test]
    public void Validate_ValidCommand_ReturnsSuccess()
    {
        ValidationResult result = _validator.Validate(new UpdateTranslationCommand("CRM0001", "es-ES", "Hola"));

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    public void Validate_EmptyCode_ReturnsFailure(string? code)
    {
        ValidationResult result = _validator.Validate(new UpdateTranslationCommand(code!, "es-ES", "Hola"));

        Assert.That(result.IsValid, Is.False);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("spanish")]
    public void Validate_InvalidLanguageCode_ReturnsFailure(string? languageCode)
    {
        ValidationResult result = _validator.Validate(new UpdateTranslationCommand("CRM0001", languageCode!, "Hola"));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyText_ReturnsFailure()
    {
        ValidationResult result = _validator.Validate(new UpdateTranslationCommand("CRM0001", "es-ES", ""));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_TextLongerThanMaximum_ReturnsFailure()
    {
        ValidationResult result = _validator.Validate(new UpdateTranslationCommand("CRM0001", "es-ES", new string('A', 4001)));

        Assert.That(result.IsValid, Is.False);
    }
}
