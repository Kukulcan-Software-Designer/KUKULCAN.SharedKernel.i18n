using KUKULCAN.SharedKernel.i18n.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.UnitTests.Services;

[TestFixture]
public sealed class I18NDomainEventDispatcherTests
{
    [Test]
    public void DispatchAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        var dispatcher = new I18NDomainEventDispatcher(new ServiceCollection().BuildServiceProvider());

        Assert.That(
            async () => await dispatcher.DispatchAsync(null!),
            Throws.TypeOf<ArgumentNullException>());
    }
}
