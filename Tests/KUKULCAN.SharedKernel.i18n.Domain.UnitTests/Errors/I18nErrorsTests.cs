using KUKULCAN.SharedKernel.i18n.Domain.Errors;
using KUKULCAN.SharedKernel.Results;

namespace KUKULCAN.SharedKernel.i18n.Domain.UnitTests.Errors;

[TestFixture]
public sealed class I18NErrorsTests
{
    [Test]
    public void Validation_CreatesError()
    {
        Error error = I18nErrors.Validation("Validation.Code", "Validation message");

        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void NotFound_CreatesError()
    {
        Error error = I18nErrors.NotFound("NotFound.Code", "Not found message");

        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void Conflict_CreatesError()
    {
        Error error = I18nErrors.Conflict("Conflict.Code", "Conflict message");

        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void Unauthorized_CreatesError()
    {
        Error error = I18nErrors.Unauthorized("Unauthorized.Code", "Unauthorized message");

        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void Forbidden_CreatesError()
    {
        Error error = I18nErrors.Forbidden("Forbidden.Code", "Forbidden message");

        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void Unexpected_CreatesError()
    {
        Error error = I18nErrors.Unexpected("Unexpected.Code", "Unexpected message");

        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void Factories_WithDifferentCodes_CreateDistinctErrors()
    {
        Error first = I18nErrors.Validation("First", "Message");
        Error second = I18nErrors.Validation("Second", "Message");

        Assert.That(first, Is.Not.EqualTo(second));
    }
}
