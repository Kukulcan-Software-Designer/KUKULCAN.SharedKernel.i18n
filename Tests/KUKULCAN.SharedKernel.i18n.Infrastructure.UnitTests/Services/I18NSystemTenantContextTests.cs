using KUKULCAN.SharedKernel.i18n.Infrastructure.Services;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.UnitTests.Services;

[TestFixture]
public sealed class I18NSystemTenantContextTests
{
    [Test]
    public void TenantId_IsEmpty()
    {
        var context = new I18NSystemTenantContext();

        Assert.That(context.TenantId, Is.EqualTo(Guid.Empty));
    }
}
