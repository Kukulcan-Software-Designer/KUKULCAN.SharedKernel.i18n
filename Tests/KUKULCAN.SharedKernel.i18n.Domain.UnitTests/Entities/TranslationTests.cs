using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.i18n.Domain.UnitTests.Entities;

[TestFixture]
public sealed class TranslationTests
{
    private static Translation CreateTranslation(string text = "Hello", int? maxLength = null)
    {
        var result = Translation.Create(Guid.NewGuid(), "CRM0001", "es-ES", text, "UI greeting", maxLength);

        Assert.That(result.IsSuccess, Is.True, result.IsFailure ? result.Error.ToString() : string.Empty);
        return result.Value;
    }

    [Test]
    public void Create_ValidInput_NormalisesAndInitialisesProperties()
    {
        var id = Guid.NewGuid();
        var result = Translation.Create(id, " crm0001 ", "ES-es", " Hello ", " Context ", 20);

        Assert.That(result.IsSuccess, Is.True);
        var translation = result.Value;

        Assert.Multiple(() =>
        {
            Assert.That(translation.Id.Value, Is.EqualTo(id));
            Assert.That(translation.Code.Value, Is.EqualTo("CRM0001"));
            Assert.That(translation.LanguageCode.Value, Is.EqualTo("es-ES"));
            Assert.That(translation.Text, Is.EqualTo("Hello"));
            Assert.That(translation.Context, Is.EqualTo("Context"));
            Assert.That(translation.MaxLength, Is.EqualTo(20));
            Assert.That(translation.IsReviewed, Is.False);
        });
    }

    [Test]
    public void Create_DefaultGuid_ReturnsFailure()
    {
        var result = Translation.Create(Guid.Empty, "CRM0001", "es-ES", "Hello");

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Create_InvalidTranslationCode_ReturnsFailure()
    {
        var result = Translation.Create(Guid.NewGuid(), "INVALID", "es-ES", "Hello");

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Create_InvalidLanguageCode_ReturnsFailure()
    {
        var result = Translation.Create(Guid.NewGuid(), "CRM0001", "invalid", "Hello");

        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Create_EmptyText_ReturnsFailure(string? text)
    {
        var result = Translation.Create(Guid.NewGuid(), "CRM0001", "es-ES", text!);

        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Create_InvalidMaxLength_ReturnsFailure(int maxLength)
    {
        var result = Translation.Create(Guid.NewGuid(), "CRM0001", "es-ES", "Hello", maxLength: maxLength);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Create_TextLongerThanMaxLength_ReturnsFailure()
    {
        var result = Translation.Create(Guid.NewGuid(), "CRM0001", "es-ES", "Hello", maxLength: 4);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void UpdateText_ValidText_TrimsAndResetsReviewStatus()
    {
        var translation = CreateTranslation();
        translation.MarkAsReviewed();

        var result = translation.UpdateText(" Updated text ");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(translation.Text, Is.EqualTo("Updated text"));
            Assert.That(translation.IsReviewed, Is.False);
        });
    }

    [Test]
    public void UpdateText_EmptyText_ReturnsFailureAndDoesNotMutate()
    {
        var translation = CreateTranslation();

        var result = translation.UpdateText(" ");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(translation.Text, Is.EqualTo("Hello"));
    }

    [Test]
    public void UpdateText_ExceedingMaxLength_ReturnsFailureAndDoesNotMutate()
    {
        var translation = CreateTranslation("Hello", 5);

        var result = translation.UpdateText("Too long");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(translation.Text, Is.EqualTo("Hello"));
    }

    [Test]
    public void UpdateContext_TrimsContext()
    {
        var translation = CreateTranslation();

        translation.UpdateContext(" New context ");

        Assert.That(translation.Context, Is.EqualTo("New context"));
    }

    [Test]
    public void UpdateContext_Null_ClearsContext()
    {
        var translation = CreateTranslation();

        translation.UpdateContext(null);

        Assert.That(translation.Context, Is.Null);
    }

    [Test]
    public void SetMaxLength_ValidValue_UpdatesConstraint()
    {
        var translation = CreateTranslation("Hello");

        var result = translation.SetMaxLength(10);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(translation.MaxLength, Is.EqualTo(10));
    }

    [Test]
    public void SetMaxLength_Null_RemovesConstraint()
    {
        var translation = CreateTranslation("Hello", 10);

        var result = translation.SetMaxLength(null);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(translation.MaxLength, Is.Null);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void SetMaxLength_NonPositiveValue_ReturnsFailure(int maxLength)
    {
        var translation = CreateTranslation();

        var result = translation.SetMaxLength(maxLength);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(translation.MaxLength, Is.Null);
    }

    [Test]
    public void SetMaxLength_SmallerThanCurrentText_ReturnsFailureAndDoesNotMutate()
    {
        var translation = CreateTranslation("Hello", 10);

        var result = translation.SetMaxLength(4);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(translation.MaxLength, Is.EqualTo(10));
    }

    [Test]
    public void ReviewLifecycle_CanMarkReviewedAndUnreviewed()
    {
        var translation = CreateTranslation();

        translation.MarkAsReviewed();
        Assert.That(translation.IsReviewed, Is.True);

        translation.MarkAsUnreviewed();
        Assert.That(translation.IsReviewed, Is.False);
    }
}
