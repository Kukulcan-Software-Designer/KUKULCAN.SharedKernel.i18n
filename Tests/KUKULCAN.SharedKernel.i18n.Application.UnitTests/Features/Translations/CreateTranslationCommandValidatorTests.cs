using FluentValidation.Results;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Commands.CreateTranslation;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests.Features.Translations;

[TestFixture]
public sealed class CreateTranslationCommandValidatorTests
{
    private readonly CreateTranslationCommandValidator _validator = new();

    [Test]
    public void Validate_ValidCommand_ReturnsSuccess()
    {
        ValidationResult result = _validator.Validate(new CreateTranslationCommand("CRM0001", "es-ES", "Hola"));

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("CRM")]
    [TestCase("CRM01")]
    public void Validate_InvalidCode_ReturnsFailure(string? code)
    {
        ValidationResult result = _validator.Validate(new CreateTranslationCommand(code!, "es-ES", "Hola"));

        Assert.That(result.IsValid, Is.False);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("spanish")]
    public void Validate_InvalidLanguageCode_ReturnsFailure(string? languageCode)
    {
        ValidationResult result = _validator.Validate(new CreateTranslationCommand("CRM0001", languageCode!, "Hola"));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_TextLongerThanMaximum_ReturnsFailure()
    {
        ValidationResult result = _validator.Validate(new CreateTranslationCommand("CRM0001", "es-ES", new string('A', 4001)));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_MaxLengthGreaterThanZeroWithShortEnoughText_ReturnsSuccess()
    {
        ValidationResult result = _validator.Validate(new CreateTranslationCommand("CRM0001", "es-ES", "Hola", MaxLength: 10));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_MaxLengthZero_ReturnsFailure()
    {
        ValidationResult result = _validator.Validate(new CreateTranslationCommand("CRM0001", "es-ES", "Hola", MaxLength: 0));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_TextExceedsMaxLength_ReturnsFailure()
    {
        ValidationResult result = _validator.Validate(new CreateTranslationCommand("CRM0001", "es-ES", "Hola mundo", MaxLength: 4));

        Assert.That(result.IsValid, Is.False);
    }
}
