using KUKULCAN.SharedKernel.i18n.Domain.Errors;

namespace KUKULCAN.SharedKernel.i18n.Domain.UnitTests.Errors;

[TestFixture]
public sealed class I18nGuardTests
{
    [Test]
    public void Null_WithNonNullReference_ReturnsSameReference()
    {
        var value = new object();

        var result = I18nGuard.Null(value, "value");

        Assert.That(result, Is.SameAs(value));
    }

    [Test]
    public void Null_WithNullReference_ThrowsArgumentNullException()
    {
        object? value = null;

        var exception = Assert.Throws<ArgumentNullException>(() => I18nGuard.Null(value, "value"));

        Assert.That(exception!.ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void NullOrWhiteSpace_WithNonEmptyValue_ReturnsSameValue()
    {
        const string value = "text";

        var result = I18nGuard.NullOrWhiteSpace(value, "value");

        Assert.That(result, Is.SameAs(value));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("   \t\r\n")]
    public void NullOrWhiteSpace_WithNullEmptyOrWhitespace_ThrowsArgumentException(string? value)
    {
        var exception = Assert.Throws<ArgumentException>(() => I18nGuard.NullOrWhiteSpace(value, "value"));

        Assert.That(exception!.ParamName, Is.EqualTo("value"));
    }
}
