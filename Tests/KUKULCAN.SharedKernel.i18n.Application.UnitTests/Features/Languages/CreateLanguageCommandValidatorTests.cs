using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Commands.CreateLanguage;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests.Features.Languages;

[TestFixture]
public sealed class CreateLanguageCommandValidatorTests
{
    private readonly CreateLanguageCommandValidator _validator = new();

    [Test]
    public void Validate_ValidCommand_ReturnsSuccess()
    {
        var result = _validator.Validate(new CreateLanguageCommand("es-ES", "Spanish", "Español"));

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("english")]
    public void Validate_InvalidCode_ReturnsFailure(string? code)
    {
        var result = _validator.Validate(new CreateLanguageCommand(code!, "Spanish", "Español"));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(CreateLanguageCommand.Code)), Is.True);
    }

    [Test]
    public void Validate_NameLongerThanMaximum_ReturnsFailure()
    {
        var result = _validator.Validate(new CreateLanguageCommand("es-ES", new string('A', 101), "Español"));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(CreateLanguageCommand.Name)), Is.True);
    }

    [Test]
    public void Validate_NativeNameLongerThanMaximum_ReturnsFailure()
    {
        var result = _validator.Validate(new CreateLanguageCommand("es-ES", "Spanish", new string('A', 101)));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(CreateLanguageCommand.NativeName)), Is.True);
    }

    [TestCase("")]
    [TestCase(" ")]
    public void Validate_EmptyNames_ReturnsFailure(string value)
    {
        var result = _validator.Validate(new CreateLanguageCommand("es-ES", value, value));

        Assert.That(result.IsValid, Is.False);
    }
}
