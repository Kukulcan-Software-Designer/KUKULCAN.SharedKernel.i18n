using KUKULCAN.SharedKernel.i18n.Infrastructure.Services;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.UnitTests.Services;

[TestFixture]
public sealed class SystemDateTimeProviderTests
{
    [Test]
    public void UtcNow_IsUtcAndCloseToCurrentTime()
    {
        DateTimeOffset before = DateTimeOffset.UtcNow;
        DateTimeOffset value = new SystemDateTimeProvider().UtcNow;
        DateTimeOffset after = DateTimeOffset.UtcNow;

        Assert.That(value.Offset, Is.EqualTo(TimeSpan.Zero));
        Assert.That(value, Is.GreaterThanOrEqualTo(before));
        Assert.That(value, Is.LessThanOrEqualTo(after));
    }

    [Test]
    public void Today_MatchesUtcNowDate()
    {
        var provider = new SystemDateTimeProvider();

        Assert.That(provider.Today, Is.EqualTo(DateOnly.FromDateTime(provider.UtcNow.UtcDateTime)));
    }

    [Test]
    public void UnixTimestampSeconds_MatchesUtcNow()
    {
        var provider = new SystemDateTimeProvider();

        Assert.That(provider.UnixTimestampSeconds, Is.EqualTo(provider.UtcNow.ToUnixTimeSeconds()));
    }
}
