using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.Results;

namespace KUKULCAN.SharedKernel.i18n.Domain.UnitTests.Entities;

[TestFixture]
public sealed class TranslationTests
{
    private static Translation CreateTranslation(string text = "Hello", int? maxLength = null)
    {
        Result<Translation> result = Translation.Create(Guid.NewGuid(), "CRM0001", "es-ES", text, "UI greeting", maxLength);

        Assert.That(result.IsSuccess, Is.True, result.IsFailure ? result.Error.ToString() : string.Empty);
        return result.Value;
    }

    [Test]
    public void Create_ValidInput_NormalisesAndInitialisesProperties()
    {
        var id = Guid.NewGuid();
        Result<Translation> result = Translation.Create(id, " crm0001 ", "ES-es", " Hello ", " Context ", 20);

        Assert.That(result.IsSuccess, Is.True);
        Translation translation = result.Value;

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
        Result<Translation> result = Translation.Create(Guid.Empty, "CRM0001", "es-ES", "Hello");

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Create_InvalidTranslationCode_ReturnsFailure()
    {
        Result<Translation> result = Translation.Create(Guid.NewGuid(), "INVALID", "es-ES", "Hello");

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Create_InvalidLanguageCode_ReturnsFailure()
    {
        Result<Translation> result = Translation.Create(Guid.NewGuid(), "CRM0001", "invalid", "Hello");

        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Create_EmptyText_ReturnsFailure(string? text)
    {
        Result<Translation> result = Translation.Create(Guid.NewGuid(), "CRM0001", "es-ES", text!);

        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Create_InvalidMaxLength_ReturnsFailure(int maxLength)
    {
        Result<Translation> result = Translation.Create(Guid.NewGuid(), "CRM0001", "es-ES", "Hello", maxLength: maxLength);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Create_TextLongerThanMaxLength_ReturnsFailure()
    {
        Result<Translation> result = Translation.Create(Guid.NewGuid(), "CRM0001", "es-ES", "Hello", maxLength: 4);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void UpdateText_ValidText_TrimsAndResetsReviewStatus()
    {
        Translation translation = CreateTranslation();
        translation.MarkAsReviewed();

        Result result = translation.UpdateText(" Updated text ");

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
        Translation translation = CreateTranslation();

        Result result = translation.UpdateText(" ");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(translation.Text, Is.EqualTo("Hello"));
    }

    [Test]
    public void UpdateText_ExceedingMaxLength_ReturnsFailureAndDoesNotMutate()
    {
        Translation translation = CreateTranslation("Hello", 5);

        Result result = translation.UpdateText("Too long");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(translation.Text, Is.EqualTo("Hello"));
    }

    [Test]
    public void UpdateContext_TrimsContext()
    {
        Translation translation = CreateTranslation();

        translation.UpdateContext(" New context ");

        Assert.That(translation.Context, Is.EqualTo("New context"));
    }

    [Test]
    public void UpdateContext_Null_ClearsContext()
    {
        Translation translation = CreateTranslation();

        translation.UpdateContext(null);

        Assert.That(translation.Context, Is.Null);
    }

    [Test]
    public void SetMaxLength_ValidValue_UpdatesConstraint()
    {
        Translation translation = CreateTranslation("Hello");

        Result result = translation.SetMaxLength(10);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(translation.MaxLength, Is.EqualTo(10));
    }

    [Test]
    public void SetMaxLength_Null_RemovesConstraint()
    {
        Translation translation = CreateTranslation("Hello", 10);

        Result result = translation.SetMaxLength(null);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(translation.MaxLength, Is.Null);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void SetMaxLength_NonPositiveValue_ReturnsFailure(int maxLength)
    {
        Translation translation = CreateTranslation();

        Result result = translation.SetMaxLength(maxLength);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(translation.MaxLength, Is.Null);
    }

    [Test]
    public void SetMaxLength_SmallerThanCurrentText_ReturnsFailureAndDoesNotMutate()
    {
        Translation translation = CreateTranslation("Hello", 10);

        Result result = translation.SetMaxLength(4);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(translation.MaxLength, Is.EqualTo(10));
    }

    [Test]
    public void ReviewLifecycle_CanMarkReviewedAndUnreviewed()
    {
        Translation translation = CreateTranslation();

        translation.MarkAsReviewed();
        Assert.That(translation.IsReviewed, Is.True);

        translation.MarkAsUnreviewed();
        Assert.That(translation.IsReviewed, Is.False);
    }
}
