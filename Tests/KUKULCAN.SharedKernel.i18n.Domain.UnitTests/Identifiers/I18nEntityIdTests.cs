using KUKULCAN.SharedKernel.i18n.Domain.Identifiers;

namespace KUKULCAN.SharedKernel.i18n.Domain.UnitTests.Identifiers;

[TestFixture]
public sealed class I18nEntityIdTests
{
    [Test]
    public void GuidConstructor_PreservesValue()
    {
        var guid = Guid.NewGuid();
        var id = new I18nEntityId(guid);

        Assert.That(id.Value, Is.EqualTo(guid));
    }

    [Test]
    public void ParameterlessConstructor_UsesDefaultGuid()
    {
        var id = new I18nEntityId();

        Assert.That(id.Value, Is.EqualTo(Guid.Empty));
    }

    [Test]
    public void ImplicitGuidConversion_CreatesIdentifier()
    {
        var guid = Guid.NewGuid();

        I18nEntityId id = guid;

        Assert.That(id.Value, Is.EqualTo(guid));
    }

    [Test]
    public void ImplicitIdentifierConversion_ReturnsGuid()
    {
        var guid = Guid.NewGuid();
        var id = new I18nEntityId(guid);

        Guid converted = id;

        Assert.That(converted, Is.EqualTo(guid));
    }

    [Test]
    public void EqualityOperators_CompareUnderlyingGuid()
    {
        var guid = Guid.NewGuid();
        var first = new I18nEntityId(guid);
        var second = new I18nEntityId(guid);
        var third = new I18nEntityId(Guid.NewGuid());

        Assert.That(first == second, Is.True);
        Assert.That(first != second, Is.False);
        Assert.That(first == third, Is.False);
        Assert.That(first != third, Is.True);
    }

    [Test]
    public void Equals_WorksWithSameAndDifferentIdentifiers()
    {
        var guid = Guid.NewGuid();
        var first = new I18nEntityId(guid);
        var same = new I18nEntityId(guid);
        var different = new I18nEntityId(Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(first.Equals(same), Is.True);
            Assert.That(first.Equals(different), Is.False);
            Assert.That(first.Equals(null), Is.False);
            Assert.That(first.Equals(guid), Is.False);
        });
    }

    [Test]
    public void EqualIdentifiers_HaveEqualHashCodes()
    {
        var guid = Guid.NewGuid();

        Assert.That(new I18nEntityId(guid).GetHashCode(),
            Is.EqualTo(new I18nEntityId(guid).GetHashCode()));
    }
}
