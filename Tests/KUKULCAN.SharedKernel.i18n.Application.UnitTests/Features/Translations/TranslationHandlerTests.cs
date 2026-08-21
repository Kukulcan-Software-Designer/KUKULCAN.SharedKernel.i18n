using KUKULCAN.SharedKernel.i18n.Application.Common;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Commands.BulkUpsertTranslations;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Commands.CreateTranslation;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Commands.DeleteTranslation;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Commands.SetTranslationReviewed;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Commands.UpdateTranslation;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Queries.GetTranslation;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Queries.GetTranslationVariants;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Queries.GetTranslationsByModule;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Queries.GetTranslationsPaged;
using KUKULCAN.SharedKernel.i18n.Domain.DTOs;
using Moq;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests.Features.Translations;

[TestFixture]
public sealed class TranslationHandlerTests
{
    [Test]
    public async Task CreateTranslation_Success_AddsSavesAndInvalidatesCaches()
    {
        var translations = new Mock<ITranslationRepository>(); var languages = new Mock<ILanguageRepository>(); var uow = new Mock<IUnitOfWork>(); var cache = new Mock<ICacheService>();
        languages.Setup(x => x.GetByCodeAsync("es-ES", It.IsAny<CancellationToken>())).ReturnsAsync(ApplicationTestData.Language());
        translations.Setup(x => x.ExistsAsync(It.IsAny<TranslationCode>(), It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = new CreateTranslationCommandHandler(translations.Object, languages.Object, uow.Object, cache.Object);
        var result = await sut.Handle(new CreateTranslationCommand("CRM0001", "es-ES", "Hola"), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); Assert.That(result.Value.Text, Is.EqualTo("Hola"));
        translations.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.Translation>(), It.IsAny<CancellationToken>()), Times.Once); uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Test]
    public async Task CreateTranslation_Duplicate_ReturnsConflictWithoutPersistence()
    {
        var translations = new Mock<ITranslationRepository>(); var languages = new Mock<ILanguageRepository>(); var uow = new Mock<IUnitOfWork>();
        languages.Setup(x => x.GetByCodeAsync("es-ES", It.IsAny<CancellationToken>())).ReturnsAsync(ApplicationTestData.Language());
        translations.Setup(x => x.ExistsAsync(It.IsAny<TranslationCode>(), It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = new CreateTranslationCommandHandler(translations.Object, languages.Object, uow.Object, new Mock<ICacheService>().Object);
        var result = await sut.Handle(new CreateTranslationCommand("CRM0001", "es-ES", "Hola"), CancellationToken.None);
        Assert.That(result.Error.Code, Is.EqualTo("Translation.Duplicate")); translations.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.Translation>(), It.IsAny<CancellationToken>()), Times.Never); uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateTranslation_InactiveLanguage_ReturnsConflict()
    {
        var languages = new Mock<ILanguageRepository>(); languages.Setup(x => x.GetByCodeAsync("es-ES", It.IsAny<CancellationToken>())).ReturnsAsync(ApplicationTestData.Language(active: false));
        var sut = new CreateTranslationCommandHandler(new Mock<ITranslationRepository>().Object, languages.Object, new Mock<IUnitOfWork>().Object, new Mock<ICacheService>().Object);
        var result = await sut.Handle(new CreateTranslationCommand("CRM0001", "es-ES", "Hola"), CancellationToken.None);
        Assert.That(result.Error.Code, Is.EqualTo("Language.Inactive"));
    }

    [Test]
    public async Task UpdateTranslation_Success_UpdatesAndInvalidatesCaches()
    {
        var repo = new Mock<ITranslationRepository>(); var uow = new Mock<IUnitOfWork>(); var cache = new Mock<ICacheService>(); var translation = ApplicationTestData.Translation();
        repo.Setup(x => x.FindAsync(It.IsAny<TranslationCode>(), It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>())).ReturnsAsync(translation);
        var sut = new UpdateTranslationCommandHandler(repo.Object, uow.Object, cache.Object); var result = await sut.Handle(new UpdateTranslationCommand("CRM0001", "es-ES", "Adiós", "updated"), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); Assert.That(translation.Text, Is.EqualTo("Adiós")); Assert.That(translation.Context, Is.EqualTo("updated")); repo.Verify(x => x.Update(translation), Times.Once); uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateTranslation_Missing_ReturnsNotFound()
    {
        var repo = new Mock<ITranslationRepository>(); var sut = new UpdateTranslationCommandHandler(repo.Object, new Mock<IUnitOfWork>().Object, new Mock<ICacheService>().Object);
        var result = await sut.Handle(new UpdateTranslationCommand("CRM0001", "es-ES", "Adiós"), CancellationToken.None); Assert.That(result.Error.Code, Is.EqualTo("Translation.NotFound"));
    }

    [Test]
    public async Task DeleteTranslation_EnglishIsProtected()
    {
        var sut = new DeleteTranslationCommandHandler(new Mock<ITranslationRepository>().Object, new Mock<IUnitOfWork>().Object, new Mock<ICacheService>().Object);
        var result = await sut.Handle(new DeleteTranslationCommand("CRM0001", "en-US"), CancellationToken.None); Assert.That(result.Error.Code, Is.EqualTo("Translation.English.ProtectedDelete"));
    }

    [Test]
    public async Task DeleteTranslation_Success_RemovesAndSaves()
    {
        var repo = new Mock<ITranslationRepository>(); var uow = new Mock<IUnitOfWork>(); var cache = new Mock<ICacheService>(); var translation = ApplicationTestData.Translation(language: "es-ES");
        repo.Setup(x => x.FindAsync(It.IsAny<TranslationCode>(), It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>())).ReturnsAsync(translation);
        var sut = new DeleteTranslationCommandHandler(repo.Object, uow.Object, cache.Object); var result = await sut.Handle(new DeleteTranslationCommand("CRM0001", "es-ES"), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); repo.Verify(x => x.Remove(translation), Times.Once); uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SetTranslationReviewed_TogglesReviewState()
    {
        var repo = new Mock<ITranslationRepository>(); var uow = new Mock<IUnitOfWork>(); var translation = ApplicationTestData.Translation();
        repo.Setup(x => x.FindAsync(It.IsAny<TranslationCode>(), It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>())).ReturnsAsync(translation);
        var sut = new SetTranslationReviewedCommandHandler(repo.Object, uow.Object); var result = await sut.Handle(new SetTranslationReviewedCommand("CRM0001", "es-ES", true), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); Assert.That(translation.IsReviewed, Is.True); repo.Verify(x => x.Update(translation), Times.Once); uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetTranslation_ReturnsCachedDto()
    {
        var cache = new Mock<ICacheService>(); var dto = new TranslationLookupDto("CRM0001", "es-ES", "Hola", false, "es-ES");
        cache.Setup(x => x.GetOrCreateAsync<TranslationLookupDto?>(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<TranslationLookupDto?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        var sut = new GetTranslationQueryHandler(new Mock<ITranslationLookupService>().Object, cache.Object); var result = await sut.Handle(new GetTranslationQuery("CRM0001", "es-ES"), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); Assert.That(result.Value.Text, Is.EqualTo("Hola"));
    }

    [Test]
    public async Task GetTranslation_CacheMissResultNull_ReturnsNotFound()
    {
        var cache = new Mock<ICacheService>(); cache.Setup(x => x.GetOrCreateAsync<TranslationLookupDto?>(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<TranslationLookupDto?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())).ReturnsAsync((TranslationLookupDto?)null);
        var sut = new GetTranslationQueryHandler(new Mock<ITranslationLookupService>().Object, cache.Object); var result = await sut.Handle(new GetTranslationQuery("CRM0001", "es-ES"), CancellationToken.None);
        Assert.That(result.Error.Code, Is.EqualTo("Translation.NotFound"));
    }

    [Test]
    public async Task GetTranslationsByModule_FillsMissingEntriesFromFallback()
    {
        var repo = new Mock<ITranslationRepository>(); var requested = new[] { ApplicationTestData.Translation("CRM0001", "es-ES", "Hola") }; var fallback = new[] { ApplicationTestData.Translation("CRM0002", "en-US", "Hello") };
        repo.Setup(x => x.GetByModuleAndLanguageAsync("CRM", It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>())).ReturnsAsync((LanguageCode lang) => lang.Value == "es-ES" ? requested : fallback);
        var sut = new GetTranslationsByModuleQueryHandler(repo.Object); var result = await sut.Handle(new GetTranslationsByModuleQuery("CRM", "es-ES"), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); Assert.That(result.Value.Translations["CRM0001"], Is.EqualTo("Hola")); Assert.That(result.Value.Translations["CRM0002"], Is.EqualTo("Hello"));
    }

    [Test]
    public async Task GetTranslationVariants_ReturnsMappedItems()
    {
        var repo = new Mock<ITranslationRepository>(); repo.Setup(x => x.GetVariantsAsync(It.IsAny<TranslationCode>(), It.IsAny<CancellationToken>())).ReturnsAsync(new[] { ApplicationTestData.Translation(), ApplicationTestData.Translation("CRM0001", "en-US", "Hello") });
        var sut = new GetTranslationVariantsQueryHandler(repo.Object); var result = await sut.Handle(new GetTranslationVariantsQuery("CRM0001"), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetTranslationsPaged_PassesFiltersAndReturnsPagedResult()
    {
        var repo = new Mock<ITranslationRepository>(); repo.Setup(x => x.GetPagedAsync(2, 10, "CRM", "es-es", It.IsAny<CancellationToken>())).ReturnsAsync((new[] { ApplicationTestData.Translation() }, 11));
        var sut = new GetTranslationsPagedQueryHandler(repo.Object); var result = await sut.Handle(new GetTranslationsPagedQuery(PaginationRequest.Create(2, 10), "crm", "ES-ES"), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); Assert.That(result.Value.TotalCount, Is.EqualTo(11)); repo.Verify(x => x.GetPagedAsync(2, 10, "CRM", "es-es", It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed record BulkItem(string Code, string LanguageCode, string Text, string? Context = null, int? MaxLength = null) : BulkTranslationDto(Code, LanguageCode, Text, Context, MaxLength);

    [Test]
    public async Task BulkUpsert_CreatesNewTranslationAndInvalidatesModuleCache()
    {
        var repo = new Mock<ITranslationRepository>(); var langs = new Mock<ILanguageRepository>(); var uow = new Mock<IUnitOfWork>(); var cache = new Mock<ICacheService>();
        langs.Setup(x => x.GetAllActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { ApplicationTestData.Language() });
        repo.Setup(x => x.FindAsync(It.IsAny<TranslationCode>(), It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Translation?)null);
        var sut = new BulkUpsertTranslationsCommandHandler(repo.Object, langs.Object, uow.Object, cache.Object); var result = await sut.Handle(new BulkUpsertTranslationsCommand(new BulkTranslationDto[] { new BulkItem("CRM0001", "es-ES", "Hola") }), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); Assert.That(result.Value.Created, Is.EqualTo(1)); repo.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.Translation>(), It.IsAny<CancellationToken>()), Times.Once); uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once); cache.Verify(x => x.RemoveAsync(I18NCacheKeys.ModuleTranslations("CRM", "es-ES"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
