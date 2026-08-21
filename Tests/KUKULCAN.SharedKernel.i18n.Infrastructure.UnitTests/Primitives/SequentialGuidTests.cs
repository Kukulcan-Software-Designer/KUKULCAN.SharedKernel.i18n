using KUKULCAN.SharedKernel.i18n.Infrastructure.Primitives;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.UnitTests.Primitives;

[TestFixture]
public sealed class SequentialGuidTests
{
    [Test]
    public void NewSequentialGuidAtEnd_ReturnsNonDefaultGuid()
    {
        Guid result = SequentialGuid.NewSequentialGuidAtEnd();

        Assert.That(result, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void NewSequentialGuidAtEnd_ReturnsUniqueValues()
    {
        Guid first = SequentialGuid.NewSequentialGuidAtEnd();
        Guid second = SequentialGuid.NewSequentialGuidAtEnd();

        Assert.That(second, Is.Not.EqualTo(first));
    }

    [Test]
    public void NewSequentialGuidAtEnd_ReturnsVersion7Guid()
    {
        Guid result = SequentialGuid.NewSequentialGuidAtEnd();
        string[] parts = result.ToString("D").Split('-');

        Assert.That(parts, Has.Length.EqualTo(5));
        Assert.That(parts[2], Does.StartWith("7"));
    }
}
