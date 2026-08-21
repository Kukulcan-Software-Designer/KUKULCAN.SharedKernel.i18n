using KUKULCAN.SharedKernel.i18n.Application.Abstractions;
using KUKULCAN.SharedKernel.i18n.Application.Common;
using KUKULCAN.SharedKernel.i18n.Application.Features.Currencies.Commands.DeleteCurrencyFormat;
using KUKULCAN.SharedKernel.i18n.Application.Features.Currencies.Commands.UpsertCurrencyFormat;
using KUKULCAN.SharedKernel.i18n.Application.Features.Currencies.Queries.GetCurrencyFormats;
using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Domain.Interfaces.Repositories;
using KUKULCAN.SharedKernel.i18n.Domain.Interfaces.Services;
using KUKULCAN.SharedKernel.i18n.Domain.ValueObjects;
using KUKULCAN.SharedKernel.i18n.Domain.ValueObjects.Enums;
using KUKULCAN.SharedKernel.Results;
using Moq;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests.Features.Currencies;

[TestFixture]
public sealed class CurrencyHandlerTests
{
    private static UpsertCurrencyFormatCommand Command(string language = "es-ES", string currency = "EUR") =>
        new(language, currency, "Euro", "€", "Before", true, ",", ".", 2, "-{symbol}{amount}");

    private static CurrencyFormat Currency(string language = "es-ES", string code = "EUR") =>
        CurrencyFormat.Create(Guid.CreateVersion7(), language, code, "Euro", "€", CurrencySymbolPosition.Before, true, ',', '.', 2, "-{symbol}{amount}").Value;

    [Test]
    public async Task Upsert_NewFormat_AddsSavesAndInvalidatesCaches()
    {
        var repo = new Mock<ICurrencyFormatRepository>();
        var languages = new Mock<ILanguageRepository>();
        var uow = new Mock<IUnitOfWork>();
        var cache = new Mock<ICacheService>();
        languages.Setup(x => x.ExistsByCodeAsync("es-ES", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(x => x.FindAsync(It.IsAny<LanguageCode>(), "EUR", It.IsAny<CancellationToken>())).ReturnsAsync((CurrencyFormat?)null);

        var sut = new UpsertCurrencyFormatCommandHandler(repo.Object, languages.Object, uow.Object, cache.Object);
        Result<Domain.DTOs.CurrencyFormatDto> result = await sut.Handle(Command(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.CurrencyCode, Is.EqualTo("EUR"));
        repo.Verify(x => x.AddAsync(It.IsAny<CurrencyFormat>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(x => x.Update(It.IsAny<CurrencyFormat>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync(I18NCacheKeys.CurrencyFormat("es-ES", "EUR"), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync(I18NCacheKeys.CurrencyFormats("es-ES"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Upsert_ExistingFormat_UpdatesInsteadOfAdds()
    {
        var repo = new Mock<ICurrencyFormatRepository>();
        var languages = new Mock<ILanguageRepository>();
        var uow = new Mock<IUnitOfWork>();
        var existing = Currency();
        languages.Setup(x => x.ExistsByCodeAsync("es-ES", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(x => x.FindAsync(It.IsAny<LanguageCode>(), "EUR", It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var sut = new UpsertCurrencyFormatCommandHandler(repo.Object, languages.Object, uow.Object, new Mock<ICacheService>().Object);
        Result<Domain.DTOs.CurrencyFormatDto> result = await sut.Handle(Command(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        repo.Verify(x => x.Update(existing), Times.Once);
        repo.Verify(x => x.AddAsync(It.IsAny<CurrencyFormat>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Upsert_MissingLanguage_ReturnsNotFoundWithoutPersistence()
    {
        var languages = new Mock<ILanguageRepository>();
        var uow = new Mock<IUnitOfWork>();
        languages.Setup(x => x.ExistsByCodeAsync("es-ES", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = new UpsertCurrencyFormatCommandHandler(new Mock<ICurrencyFormatRepository>().Object, languages.Object, uow.Object, new Mock<ICacheService>().Object);
        Result<Domain.DTOs.CurrencyFormatDto> result = await sut.Handle(Command(), CancellationToken.None);

        Assert.That(result.Error.Code, Is.EqualTo("Language.NotFound"));
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Upsert_InvalidLanguageCode_ReturnsFailureBeforeRepositoryAccess()
    {
        var languages = new Mock<ILanguageRepository>();
        var repo = new Mock<ICurrencyFormatRepository>();
        var sut = new UpsertCurrencyFormatCommandHandler(repo.Object, languages.Object, new Mock<IUnitOfWork>().Object, new Mock<ICacheService>().Object);

        Result<Domain.DTOs.CurrencyFormatDto> result = await sut.Handle(Command("invalid"), CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
        languages.Verify(x => x.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(x => x.FindAsync(It.IsAny<LanguageCode>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Upsert_InvalidDomainData_ReturnsFailureWithoutPersistence()
    {
        var languages = new Mock<ILanguageRepository>();
        var repo = new Mock<ICurrencyFormatRepository>();
        var uow = new Mock<IUnitOfWork>();
        languages.Setup(x => x.ExistsByCodeAsync("es-ES", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(x => x.FindAsync(It.IsAny<LanguageCode>(), "EUR", It.IsAny<CancellationToken>())).ReturnsAsync((CurrencyFormat?)null);

        var sut = new UpsertCurrencyFormatCommandHandler(repo.Object, languages.Object, uow.Object, new Mock<ICacheService>().Object);
        Result<Domain.DTOs.CurrencyFormatDto> result = await sut.Handle(Command() with { CurrencyName = "" }, CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
        repo.Verify(x => x.AddAsync(It.IsAny<CurrencyFormat>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Delete_ExistingFormat_RemovesSavesAndInvalidatesCaches()
    {
        var repo = new Mock<ICurrencyFormatRepository>();
        var uow = new Mock<IUnitOfWork>();
        var cache = new Mock<ICacheService>();
        var format = Currency();
        repo.Setup(x => x.FindAsync(It.IsAny<LanguageCode>(), "EUR", It.IsAny<CancellationToken>())).ReturnsAsync(format);

        var sut = new DeleteCurrencyFormatCommandHandler(repo.Object, uow.Object, cache.Object);
        Result result = await sut.Handle(new DeleteCurrencyFormatCommand("es-ES", "eur"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        repo.Verify(x => x.Remove(format), Times.Once);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync(I18NCacheKeys.CurrencyFormat("es-ES", "EUR"), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync(I18NCacheKeys.CurrencyFormats("es-ES"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Delete_MissingFormat_ReturnsNotFoundWithoutPersistence()
    {
        var repo = new Mock<ICurrencyFormatRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(x => x.FindAsync(It.IsAny<LanguageCode>(), "EUR", It.IsAny<CancellationToken>())).ReturnsAsync((CurrencyFormat?)null);

        var sut = new DeleteCurrencyFormatCommandHandler(repo.Object, uow.Object, new Mock<ICacheService>().Object);
        Result result = await sut.Handle(new DeleteCurrencyFormatCommand("es-ES", "EUR"), CancellationToken.None);

        Assert.That(result.Error.Code, Is.EqualTo("CurrencyFormat.NotFound"));
        repo.Verify(x => x.Remove(It.IsAny<CurrencyFormat>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Delete_InvalidLanguageCode_ReturnsFailureWithoutRepositoryAccess()
    {
        var repo = new Mock<ICurrencyFormatRepository>();
        var sut = new DeleteCurrencyFormatCommandHandler(repo.Object, new Mock<IUnitOfWork>().Object, new Mock<ICacheService>().Object);

        Result result = await sut.Handle(new DeleteCurrencyFormatCommand("invalid", "EUR"), CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
        repo.Verify(x => x.FindAsync(It.IsAny<LanguageCode>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetCurrencyFormats_Found_MapsRepositoryItems()
    {
        var repo = new Mock<ICurrencyFormatRepository>();
        repo.Setup(x => x.GetByLanguageAsync(It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>())).ReturnsAsync(new[] { Currency() });
        var sut = new GetCurrencyFormatsQueryHandler(repo.Object);

        Result<IReadOnlyList<Domain.DTOs.CurrencyFormatDto>> result = await sut.Handle(new GetCurrencyFormatsQuery("es-ES"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(1));
        Assert.That(result.Value[0].CurrencyCode, Is.EqualTo("EUR"));
        Assert.That(result.Value[0].LanguageCode, Is.EqualTo("es-ES"));
        Assert.That(result.Value[0].FormattedExample, Is.Not.Empty);
        repo.Verify(x => x.GetByLanguageAsync(It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetCurrencyFormats_InvalidLanguageCode_ReturnsFailureWithoutRepositoryAccess()
    {
        var repo = new Mock<ICurrencyFormatRepository>();
        var sut = new GetCurrencyFormatsQueryHandler(repo.Object);

        Result<IReadOnlyList<Domain.DTOs.CurrencyFormatDto>> result = await sut.Handle(new GetCurrencyFormatsQuery("invalid"), CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
        repo.Verify(x => x.GetByLanguageAsync(It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void GetCurrencyFormatsQuery_ExposesExpectedCacheConfiguration()
    {
        var query = new GetCurrencyFormatsQuery("es-ES");

        Assert.That(query.CacheKey, Is.EqualTo(I18NCacheKeys.CurrencyFormats("es-ES")));
        Assert.That(query.CacheDuration, Is.EqualTo(TimeSpan.FromHours(6)));
    }
}
