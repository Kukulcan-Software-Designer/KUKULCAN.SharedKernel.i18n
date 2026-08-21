using DatabaseUnitOfWork = KUKULCAN.SharedKernel.Database.Abstractions.IUnitOfWork;
using ApplicationUnitOfWork = KUKULCAN.SharedKernel.i18n.Application.Abstractions.IUnitOfWork;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Services;
using Moq;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.UnitTests.Services;

[TestFixture]
public sealed class I18NUnitOfWorkTests
{
    private Mock<DatabaseUnitOfWork> _inner = null!;
    private I18nUnitOfWork _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _inner = new Mock<DatabaseUnitOfWork>();
        _sut = new I18nUnitOfWork(_inner.Object);
    }

    [Test]
    public async Task SaveChangesAsync_ForwardsCancellationTokenAndResult()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken token = cts.Token;
        _inner.Setup(x => x.SaveChangesAsync(token)).ReturnsAsync(7);

        int result = await _sut.SaveChangesAsync(token);

        Assert.That(result, Is.EqualTo(7));
        _inner.Verify(x => x.SaveChangesAsync(token), Times.Once);
    }

    [Test]
    public async Task TransactionMethods_ForwardCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken token = cts.Token;

        await _sut.BeginTransactionAsync(token);
        await _sut.CommitTransactionAsync(token);
        await _sut.RollbackTransactionAsync(token);
        await _sut.EndTransactionAsync(token);

        _inner.Verify(x => x.BeginTransactionAsync(token), Times.Once);
        _inner.Verify(x => x.CommitTransactionAsync(token), Times.Once);
        _inner.Verify(x => x.RollbackTransactionAsync(token), Times.Once);
        _inner.Verify(x => x.EndTransactionAsync(token), Times.Once);
    }

    [Test]
    public void Dispose_DoesNotDisposeInnerUnitOfWork()
    {
        _sut.Dispose();

        _inner.Verify(x => x.Dispose(), Times.Never);
    }

    [Test]
    public async Task DisposeAsync_CompletesWithoutDisposingInnerUnitOfWork()
    {
        await _sut.DisposeAsync();

        _inner.Verify(x => x.DisposeAsync(), Times.Never);
    }

    [Test]
    public void ImplementsApplicationUnitOfWorkContract()
    {
        Assert.That(_sut, Is.InstanceOf<ApplicationUnitOfWork>());
    }
}
