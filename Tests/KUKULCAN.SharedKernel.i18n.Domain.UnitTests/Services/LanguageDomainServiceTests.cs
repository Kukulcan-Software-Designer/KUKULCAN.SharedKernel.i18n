using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Domain.Interfaces.Repositories;
using KUKULCAN.SharedKernel.i18n.Domain.Services;
using KUKULCAN.SharedKernel.Results;
using Moq;

namespace KUKULCAN.SharedKernel.i18n.Domain.UnitTests.Services;

[TestFixture]
public sealed class LanguageDomainServiceTests
{
    private static Language CreateLanguage(string code, bool isDefault = false)
    {
        Result<Language> result = Language.Create(Guid.NewGuid(), code, code, code, isDefault);
        Assert.That(result.IsSuccess, Is.True);
        return result.Value;
    }

    [Test]
    public async Task SetDefaultLanguageAsync_WhenLanguageDoesNotExist_ReturnsFailure()
    {
        var repository = new Mock<ILanguageRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetByCodeAsync("es-ES", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Language?)null);

        var service = new LanguageDomainService(repository.Object);

        Result result = await service.SetDefaultLanguageAsync("es-ES");

        Assert.That(result.IsFailure, Is.True);
        repository.Verify(x => x.GetDefaultAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SetDefaultLanguageAsync_WhenLanguageInactive_ReturnsFailureWithoutChangingDefault()
    {
        Language inactive = CreateLanguage("es-ES");
        Assert.That(inactive.Deactivate().IsSuccess, Is.True);

        var repository = new Mock<ILanguageRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetByCodeAsync("es-ES", It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactive);

        var service = new LanguageDomainService(repository.Object);

        Result result = await service.SetDefaultLanguageAsync("es-ES");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(inactive.IsDefault, Is.False);
        repository.Verify(x => x.GetDefaultAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SetDefaultLanguageAsync_TransfersDefaultFromPreviousLanguage()
    {
        Language current = CreateLanguage("en-US", true);
        Language replacement = CreateLanguage("es-ES");

        var repository = new Mock<ILanguageRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetByCodeAsync("es-ES", It.IsAny<CancellationToken>()))
            .ReturnsAsync(replacement);
        repository.Setup(x => x.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);

        var service = new LanguageDomainService(repository.Object);

        Result result = await service.SetDefaultLanguageAsync("es-ES");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(current.IsDefault, Is.False);
            Assert.That(replacement.IsDefault, Is.True);
            Assert.That(replacement.IsActive, Is.True);
        });
    }

    [Test]
    public async Task SetDefaultLanguageAsync_WhenTargetAlreadyIsDefault_LeavesItDefault()
    {
        Language current = CreateLanguage("es-ES", true);

        var repository = new Mock<ILanguageRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetByCodeAsync("es-ES", It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);
        repository.Setup(x => x.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);

        var service = new LanguageDomainService(repository.Object);

        Result result = await service.SetDefaultLanguageAsync("es-ES");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(current.IsDefault, Is.True);
    }

    [Test]
    public async Task SetDefaultLanguageAsync_PassesCancellationTokenToRepository()
    {
        Language language = CreateLanguage("es-ES");
        CancellationToken token = new CancellationTokenSource().Token;

        var repository = new Mock<ILanguageRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetByCodeAsync("es-ES", token)).ReturnsAsync(language);
        repository.Setup(x => x.GetDefaultAsync(token)).ReturnsAsync((Language?)null);

        var service = new LanguageDomainService(repository.Object);

        Result result = await service.SetDefaultLanguageAsync("es-ES", token);

        Assert.That(result.IsSuccess, Is.True);
        repository.Verify(x => x.GetByCodeAsync("es-ES", token), Times.Once);
        repository.Verify(x => x.GetDefaultAsync(token), Times.Once);
    }
}
