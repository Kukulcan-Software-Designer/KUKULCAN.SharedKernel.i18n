using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Domain.Interfaces.Repositories;
using KUKULCAN.SharedKernel.i18n.Domain.Services;
using KUKULCAN.SharedKernel.i18n.Domain.ValueObjects;
using KUKULCAN.SharedKernel.Results;
using Moq;

namespace KUKULCAN.SharedKernel.i18n.Domain.UnitTests.Services;

[TestFixture]
public sealed class TranslationLookupServiceTests
{
    private static Translation CreateTranslation(string language, string text)
    {
        Result<Translation> result = Translation.Create(Guid.NewGuid(), "CRM0001", language, text);
        Assert.That(result.IsSuccess, Is.True);
        return result.Value;
    }

    private static TranslationCode Code => TranslationCode.From("CRM0001").Value;

    [Test]
    public async Task ResolveAsync_WhenExactTranslationExists_ReturnsExactMatchWithoutFallback()
    {
        LanguageCode requested = LanguageCode.Create("es-MX").Value;
        Translation exact = CreateTranslation("es-MX", "Hola México");

        var repository = new Mock<ITranslationRepository>(MockBehavior.Strict);
        repository.Setup(x => x.FindAsync(Code, It.Is<LanguageCode>(l => l.Value == "es-MX"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exact);

        var service = new TranslationLookupService(repository.Object);

        Result<(string Text, string ActualLanguage, bool IsFallback)> result = await service.ResolveAsync(Code, requested);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Text, Is.EqualTo("Hola México"));
            Assert.That(result.Value.ActualLanguage, Is.EqualTo("es-MX"));
            Assert.That(result.Value.IsFallback, Is.False);
        });

        repository.Verify(x => x.FindAsync(Code, It.Is<LanguageCode>(l => l.Value == "es-MX"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ResolveAsync_WhenExactMissing_UsesLanguageFallback()
    {
        LanguageCode requested = LanguageCode.Create("es-MX").Value;
        Translation parent = CreateTranslation("es", "Hola");

        var repository = new Mock<ITranslationRepository>(MockBehavior.Strict);
        repository.Setup(x => x.FindAsync(Code, It.Is<LanguageCode>(l => l.Value == "es-MX"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Translation?)null);
        repository.Setup(x => x.FindAsync(Code, It.Is<LanguageCode>(l => l.Value == "es"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);

        var service = new TranslationLookupService(repository.Object);

        Result<(string Text, string ActualLanguage, bool IsFallback)> result = await service.ResolveAsync(Code, requested);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Text, Is.EqualTo("Hola"));
            Assert.That(result.Value.ActualLanguage, Is.EqualTo("es"));
            Assert.That(result.Value.IsFallback, Is.True);
        });
    }

    [Test]
    public async Task ResolveAsync_WhenRegionalAndParentMissing_UsesEnglishFallback()
    {
        LanguageCode requested = LanguageCode.Create("es-MX").Value;
        Translation english = CreateTranslation("en", "Hello");

        var repository = new Mock<ITranslationRepository>(MockBehavior.Strict);
        repository.Setup(x => x.FindAsync(Code, It.Is<LanguageCode>(l => l.Value == "es-MX"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Translation?)null);
        repository.Setup(x => x.FindAsync(Code, It.Is<LanguageCode>(l => l.Value == "es"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Translation?)null);
        repository.Setup(x => x.FindAsync(Code, It.Is<LanguageCode>(l => l.Value == "en"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(english);

        var service = new TranslationLookupService(repository.Object);

        Result<(string Text, string ActualLanguage, bool IsFallback)> result = await service.ResolveAsync(Code, requested);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Text, Is.EqualTo("Hello"));
            Assert.That(result.Value.ActualLanguage, Is.EqualTo("en"));
            Assert.That(result.Value.IsFallback, Is.True);
        });
    }

    [Test]
    public async Task ResolveAsync_WhenNothingExists_ReturnsNotFound()
    {
        LanguageCode requested = LanguageCode.Create("es-MX").Value;

        var repository = new Mock<ITranslationRepository>(MockBehavior.Strict);
        repository.Setup(x => x.FindAsync(Code, It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Translation?)null);

        var service = new TranslationLookupService(repository.Object);

        Result<(string Text, string ActualLanguage, bool IsFallback)> result = await service.ResolveAsync(Code, requested);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task ResolveAsync_PassesCancellationTokenThroughFallbackLookups()
    {
        LanguageCode requested = LanguageCode.Create("es-MX").Value;
        CancellationToken token = new CancellationTokenSource().Token;
        Translation english = CreateTranslation("en", "Hello");

        var repository = new Mock<ITranslationRepository>(MockBehavior.Strict);
        repository.Setup(x => x.FindAsync(Code, It.Is<LanguageCode>(l => l.Value == "es-MX"), token)).ReturnsAsync((Translation?)null);
        repository.Setup(x => x.FindAsync(Code, It.Is<LanguageCode>(l => l.Value == "es"), token)).ReturnsAsync((Translation?)null);
        repository.Setup(x => x.FindAsync(Code, It.Is<LanguageCode>(l => l.Value == "en"), token)).ReturnsAsync(english);

        var service = new TranslationLookupService(repository.Object);

        Result<(string Text, string ActualLanguage, bool IsFallback)> result = await service.ResolveAsync(Code, requested, token);

        Assert.That(result.IsSuccess, Is.True);
        repository.Verify(x => x.FindAsync(Code, It.IsAny<LanguageCode>(), token), Times.Exactly(3));
    }
}
