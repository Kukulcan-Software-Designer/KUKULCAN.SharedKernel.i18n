using KUKULCAN.SharedKernel.i18n.Application.Abstractions;
using KUKULCAN.SharedKernel.i18n.Application.Common;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Commands.CreateLanguage;
using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Domain.Interfaces.Repositories;
using KUKULCAN.SharedKernel.i18n.Domain.Interfaces.Services;
using Moq;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests.Features.Languages;

[TestFixture]
public sealed class CreateLanguageCommandHandlerTests
{
    [Test]
    public async Task Handle_DuplicateCode_ReturnsConflictAndDoesNotPersist()
    {
        var repository = new Mock<ILanguageRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var cache = new Mock<ICacheService>();
        var cancellationToken = new CancellationTokenSource().Token;
        repository.Setup(x => x.ExistsByCodeAsync("es-ES", cancellationToken)).ReturnsAsync(true);

        var handler = new CreateLanguageCommandHandler(repository.Object, unitOfWork.Object, cache.Object);

        var result = await handler.Handle(new CreateLanguageCommand("es-ES", "Spanish", "Español"), cancellationToken);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Language.Duplicate"));
        repository.Verify(x => x.AddAsync(It.IsAny<Language>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        cache.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_ValidCommand_AddsLanguageSavesAndInvalidatesCaches()
    {
        var repository = new Mock<ILanguageRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var cache = new Mock<ICacheService>();
        var cancellationToken = new CancellationTokenSource().Token;
        repository.Setup(x => x.ExistsByCodeAsync("es-ES", cancellationToken)).ReturnsAsync(false);
        repository.Setup(x => x.AddAsync(It.IsAny<Language>(), cancellationToken)).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.SaveChangesAsync(cancellationToken)).ReturnsAsync(1);
        cache.Setup(x => x.RemoveAsync(It.IsAny<string>(), cancellationToken)).Returns(Task.CompletedTask);

        var handler = new CreateLanguageCommandHandler(repository.Object, unitOfWork.Object, cache.Object);

        var result = await handler.Handle(new CreateLanguageCommand("es-ES", "Spanish", "Español"), cancellationToken);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Code, Is.EqualTo("es-ES"));
        Assert.That(result.Value.Name, Is.EqualTo("Spanish"));
        repository.Verify(x => x.AddAsync(It.Is<Language>(l => l.Code == "es-ES"), cancellationToken), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(cancellationToken), Times.Once);
        cache.Verify(x => x.RemoveAsync(I18NCacheKeys.LanguagesAll, cancellationToken), Times.Once);
        cache.Verify(x => x.RemoveAsync(I18NCacheKeys.LanguagesActive, cancellationToken), Times.Once);
    }
}
