using KUKULCAN.SharedKernel.i18n.Application.Abstractions;
using KUKULCAN.SharedKernel.i18n.Application.Common;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Commands.SetDefaultLanguage;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Commands.SetLanguageActive;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Commands.UpdateLanguage;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Queries.GetAllLanguages;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Queries.GetLanguage;
using KUKULCAN.SharedKernel.i18n.Domain.Errors;
using KUKULCAN.SharedKernel.i18n.Domain.Interfaces.Repositories;
using KUKULCAN.SharedKernel.i18n.Domain.Interfaces.Services;
using KUKULCAN.SharedKernel.Results;
using Moq;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests.Features.Languages;

[TestFixture]
public sealed class LanguageHandlerTests
{
    [Test]
    public async Task SetLanguageActive_ActivatesLanguage_PersistsAndInvalidatesCaches()
    {
        var repo = new Mock<ILanguageRepository>(); var uow = new Mock<IUnitOfWork>(); var cache = new Mock<ICacheService>();
        var language = ApplicationTestData.Language(active: false);
        repo.Setup(x => x.GetByCodeAsync("es-ES", It.IsAny<CancellationToken>())).ReturnsAsync(language);
        var sut = new SetLanguageActiveCommandHandler(repo.Object, uow.Object, cache.Object);
        var result = await sut.Handle(new SetLanguageActiveCommand("es-ES", true), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); Assert.That(language.IsActive, Is.True);
        repo.Verify(x => x.Update(language), Times.Once); uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync(I18NCacheKeys.Language("es-ES"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SetLanguageActive_DefaultLanguageCannotBeDeactivated()
    {
        var repo = new Mock<ILanguageRepository>(); var uow = new Mock<IUnitOfWork>(); var cache = new Mock<ICacheService>();
        var language = ApplicationTestData.Language(isDefault: true);
        repo.Setup(x => x.GetByCodeAsync("en-US", It.IsAny<CancellationToken>())).ReturnsAsync(language);
        var sut = new SetLanguageActiveCommandHandler(repo.Object, uow.Object, cache.Object);
        var result = await sut.Handle(new SetLanguageActiveCommand("en-US", false), CancellationToken.None);
        Assert.That(result.IsFailure, Is.True); Assert.That(result.Error.Code, Is.EqualTo("Language.Default.CannotDeactivate"));
        repo.Verify(x => x.Update(It.IsAny<Domain.Entities.Language>()), Times.Never); uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SetLanguageActive_MissingLanguage_ReturnsNotFound()
    {
        var repo = new Mock<ILanguageRepository>(); var sut = new SetLanguageActiveCommandHandler(repo.Object, new Mock<IUnitOfWork>().Object, new Mock<ICacheService>().Object);
        var result = await sut.Handle(new SetLanguageActiveCommand("xx-XX", true), CancellationToken.None);
        Assert.That(result.Error.Code, Is.EqualTo("Language.NotFound"));
    }

    [Test]
    public async Task SetDefaultLanguage_Success_SavesAndInvalidatesCaches()
    {
        var domain = new Mock<ILanguageDomainService>(); var uow = new Mock<IUnitOfWork>(); var cache = new Mock<ICacheService>();
        domain.Setup(x => x.SetDefaultLanguageAsync("es-ES", It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        var sut = new SetDefaultLanguageCommandHandler(domain.Object, uow.Object, cache.Object);
        var result = await sut.Handle(new SetDefaultLanguageCommand("es-ES"), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync(I18NCacheKeys.LanguageDefault, It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync(I18NCacheKeys.LanguagesAll, It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync(I18NCacheKeys.LanguagesActive, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SetDefaultLanguage_DomainFailure_DoesNotPersist()
    {
        var domain = new Mock<ILanguageDomainService>(); var uow = new Mock<IUnitOfWork>(); var cache = new Mock<ICacheService>();
        domain.Setup(x => x.SetDefaultLanguageAsync("xx-XX", It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure(I18nErrors.NotFound("Language.NotFound", "missing")));
        var sut = new SetDefaultLanguageCommandHandler(domain.Object, uow.Object, cache.Object);
        var result = await sut.Handle(new SetDefaultLanguageCommand("xx-XX"), CancellationToken.None);
        Assert.That(result.IsFailure, Is.True); uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never); cache.VerifyNoOtherCalls();
    }

    [Test]
    public async Task UpdateLanguage_Success_UpdatesAndReturnsDto()
    {
        var repo = new Mock<ILanguageRepository>(); var uow = new Mock<IUnitOfWork>(); var cache = new Mock<ICacheService>(); var language = ApplicationTestData.Language();
        repo.Setup(x => x.GetByCodeAsync("es-ES", It.IsAny<CancellationToken>())).ReturnsAsync(language);
        var sut = new UpdateLanguageCommandHandler(repo.Object, uow.Object, cache.Object);
        var result = await sut.Handle(new UpdateLanguageCommand("es-ES", "Spanish Updated", "Español"), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); Assert.That(result.Value.Name, Is.EqualTo("Spanish Updated"));
        repo.Verify(x => x.Update(language), Times.Once); uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateLanguage_MissingLanguage_ReturnsNotFound()
    {
        var repo = new Mock<ILanguageRepository>(); var sut = new UpdateLanguageCommandHandler(repo.Object, new Mock<IUnitOfWork>().Object, new Mock<ICacheService>().Object);
        var result = await sut.Handle(new UpdateLanguageCommand("xx-XX", "X", "X"), CancellationToken.None);
        Assert.That(result.Error.Code, Is.EqualTo("Language.NotFound"));
    }

    [Test]
    public async Task UpdateLanguage_InvalidName_ReturnsValidationFailureWithoutPersistence()
    {
        var repo = new Mock<ILanguageRepository>(); var uow = new Mock<IUnitOfWork>(); var language = ApplicationTestData.Language();
        repo.Setup(x => x.GetByCodeAsync("es-ES", It.IsAny<CancellationToken>())).ReturnsAsync(language);
        var sut = new UpdateLanguageCommandHandler(repo.Object, uow.Object, new Mock<ICacheService>().Object);
        var result = await sut.Handle(new UpdateLanguageCommand("es-ES", "", "Español"), CancellationToken.None);
        Assert.That(result.IsFailure, Is.True); repo.Verify(x => x.Update(It.IsAny<Domain.Entities.Language>()), Times.Never); uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetAllLanguages_ActiveOnly_UsesActiveRepository()
    {
        var repo = new Mock<ILanguageRepository>(); repo.Setup(x => x.GetAllActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { ApplicationTestData.Language() });
        var sut = new GetAllLanguagesQueryHandler(repo.Object);
        var result = await sut.Handle(new GetAllLanguagesQuery(true), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); Assert.That(result.Value, Has.Count.EqualTo(1)); repo.Verify(x => x.GetAllActiveAsync(It.IsAny<CancellationToken>()), Times.Once); repo.Verify(x => x.ListAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetAllLanguages_All_MapsRepositoryEntities()
    {
        var repo = new Mock<ILanguageRepository>(); repo.Setup(x => x.ListAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { ApplicationTestData.Language() });
        var sut = new GetAllLanguagesQueryHandler(repo.Object);
        var result = await sut.Handle(new GetAllLanguagesQuery(false), CancellationToken.None);
        Assert.That(result.Value[0].Code, Is.EqualTo("es-ES")); repo.Verify(x => x.ListAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetLanguage_Missing_ReturnsNotFound()
    {
        var repo = new Mock<ILanguageRepository>(); var sut = new GetLanguageQueryHandler(repo.Object);
        var result = await sut.Handle(new GetLanguageQuery("xx-XX"), CancellationToken.None);
        Assert.That(result.Error.Code, Is.EqualTo("Language.NotFound"));
    }

    [Test]
    public async Task GetLanguage_Found_ReturnsDto()
    {
        var repo = new Mock<ILanguageRepository>(); repo.Setup(x => x.GetByCodeAsync("es-ES", It.IsAny<CancellationToken>())).ReturnsAsync(ApplicationTestData.Language());
        var sut = new GetLanguageQueryHandler(repo.Object); var result = await sut.Handle(new GetLanguageQuery("es-ES"), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); Assert.That(result.Value.NativeName, Is.EqualTo("Español"));
    }
}
