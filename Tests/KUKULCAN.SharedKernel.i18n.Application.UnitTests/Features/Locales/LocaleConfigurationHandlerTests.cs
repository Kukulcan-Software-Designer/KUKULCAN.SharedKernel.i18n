using KUKULCAN.SharedKernel.i18n.Application.Common;
using KUKULCAN.SharedKernel.i18n.Application.Features.Locales.Commands.UpsertLocaleConfiguration;
using KUKULCAN.SharedKernel.i18n.Application.Features.Locales.Queries.GetAllLocaleConfigurations;
using KUKULCAN.SharedKernel.i18n.Application.Features.Locales.Queries.GetLocaleConfiguration;
using Moq;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests.Features.Locales;

[TestFixture]
public sealed class LocaleConfigurationHandlerTests
{
    private static UpsertLocaleConfigurationCommand Command(string language = "es-ES") =>
        new(language, "dd/MM/yyyy", "dd/MM/yy", "HH:mm", "dd/MM/yyyy HH:mm", "Monday", ",", ".", 2, 2);

    [Test]
    public async Task Upsert_NewConfiguration_AddsSavesAndInvalidatesCache()
    {
        var repo = new Mock<ILocaleConfigurationRepository>(); var langRepo = new Mock<ILanguageRepository>(); var uow = new Mock<IUnitOfWork>(); var cache = new Mock<ICacheService>();
        langRepo.Setup(x => x.ExistsByCodeAsync("es-ES", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(x => x.GetByLanguageAsync(It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.LocaleConfiguration?)null);
        var sut = new UpsertLocaleConfigurationCommandHandler(repo.Object, langRepo.Object, uow.Object, cache.Object);
        var result = await sut.Handle(Command(), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); repo.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.LocaleConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(x => x.Update(It.IsAny<Domain.Entities.LocaleConfiguration>()), Times.Never); uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync(I18NCacheKeys.LocaleConfig("es-ES"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Upsert_ExistingConfiguration_UpdatesInsteadOfAdds()
    {
        var repo = new Mock<ILocaleConfigurationRepository>(); var langRepo = new Mock<ILanguageRepository>(); var uow = new Mock<IUnitOfWork>();
        var existing = ApplicationTestData.Locale(); langRepo.Setup(x => x.ExistsByCodeAsync("es-ES", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(x => x.GetByLanguageAsync(It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var sut = new UpsertLocaleConfigurationCommandHandler(repo.Object, langRepo.Object, uow.Object, new Mock<ICacheService>().Object);
        var result = await sut.Handle(Command(), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); repo.Verify(x => x.Update(existing), Times.Once); repo.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.LocaleConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Upsert_MissingLanguage_ReturnsNotFoundWithoutPersistence()
    {
        var langRepo = new Mock<ILanguageRepository>(); var uow = new Mock<IUnitOfWork>(); langRepo.Setup(x => x.ExistsByCodeAsync("es-ES", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = new UpsertLocaleConfigurationCommandHandler(new Mock<ILocaleConfigurationRepository>().Object, langRepo.Object, uow.Object, new Mock<ICacheService>().Object);
        var result = await sut.Handle(Command(), CancellationToken.None);
        Assert.That(result.Error.Code, Is.EqualTo("Language.NotFound")); uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Upsert_InvalidLanguageCode_ReturnsFailureBeforeRepositoryAccess()
    {
        var langRepo = new Mock<ILanguageRepository>(); var sut = new UpsertLocaleConfigurationCommandHandler(new Mock<ILocaleConfigurationRepository>().Object, langRepo.Object, new Mock<IUnitOfWork>().Object, new Mock<ICacheService>().Object);
        var result = await sut.Handle(Command("bad"), CancellationToken.None);
        Assert.That(result.IsFailure, Is.True); langRepo.Verify(x => x.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetAllLocaleConfigurations_MapsAllItems()
    {
        var repo = new Mock<ILocaleConfigurationRepository>(); repo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { ApplicationTestData.Locale() });
        var sut = new GetAllLocaleConfigurationsQueryHandler(repo.Object); var result = await sut.Handle(new GetAllLocaleConfigurationsQuery(), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); Assert.That(result.Value, Has.Count.EqualTo(1)); Assert.That(result.Value[0].LanguageCode, Is.EqualTo("es-ES"));
    }

    [Test]
    public async Task GetLocaleConfiguration_InvalidLanguageCode_ReturnsFailure()
    {
        var repo = new Mock<ILocaleConfigurationRepository>(); var sut = new GetLocaleConfigurationQueryHandler(repo.Object);
        var result = await sut.Handle(new GetLocaleConfigurationQuery("bad"), CancellationToken.None);
        Assert.That(result.IsFailure, Is.True); repo.Verify(x => x.GetByLanguageAsync(It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetLocaleConfiguration_Missing_ReturnsNotFound()
    {
        var repo = new Mock<ILocaleConfigurationRepository>(); repo.Setup(x => x.GetByLanguageAsync(It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.LocaleConfiguration?)null);
        var sut = new GetLocaleConfigurationQueryHandler(repo.Object); var result = await sut.Handle(new GetLocaleConfigurationQuery("es-ES"), CancellationToken.None);
        Assert.That(result.Error.Code, Is.EqualTo("LocaleConfig.NotFound"));
    }

    [Test]
    public async Task GetLocaleConfiguration_Found_ReturnsMappedDto()
    {
        var repo = new Mock<ILocaleConfigurationRepository>(); var config = ApplicationTestData.Locale(); repo.Setup(x => x.GetByLanguageAsync(It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>())).ReturnsAsync(config);
        var sut = new GetLocaleConfigurationQueryHandler(repo.Object); var result = await sut.Handle(new GetLocaleConfigurationQuery("es-ES"), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); Assert.That(result.Value.DecimalSeparator, Is.EqualTo(",")); Assert.That(result.Value.FirstDayOfWeek, Is.EqualTo("Monday"));
    }
}
