using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.UnitTests.Persistence.Repositories;

[TestFixture]
public sealed class TranslationRepositoryTests
{
    [SetUp]
    public Task SetUp() => RepositoryTestDatabase.ResetAsync();

    private static async Task<I18NDbContext> ContextWithLanguagesAsync()
    {
        I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();

        c.Languages
            .AddRange(RepositoryTestData.Language(), RepositoryTestData.Language("en-US", "English"));
        await c.SaveChangesAsync();

        return c;
    }

    [Test]
    public async Task GetByIdAsync_ReturnsMatchingTranslation()
    {
        await using I18NDbContext c = await ContextWithLanguagesAsync();
        Translation e = RepositoryTestData.Translation();

        c.Translations.Add(e);
        await c.SaveChangesAsync();

        Assert.That((await new TranslationRepository(c).GetByIdAsync(e.Id.Value))?.Id.Value, Is.EqualTo(e.Id.Value));
    }

    [Test]
    public async Task FindAsync_ReturnsMatchingTranslationByCodeAndLanguage()
    {
        await using I18NDbContext c = await ContextWithLanguagesAsync();
        Translation e = RepositoryTestData.Translation();

        c.Translations.Add(e);
        await c.SaveChangesAsync();

        Assert.That((await new TranslationRepository(c).FindAsync(RepositoryTestData.TranslationCode("CRM0001"), RepositoryTestData.LanguageCode("es-ES")))?.Id.Value, Is.EqualTo(e.Id.Value));
    }

    [Test]
    public async Task ListAllAsync_ReturnsTranslationsOrderedByCodeAndLanguage()
    {
        await using I18NDbContext c = await ContextWithLanguagesAsync();

        c.Translations.AddRange(RepositoryTestData.Translation("CRM0002", "es-ES"), RepositoryTestData.Translation("CRM0001", "en-US"));
        await c.SaveChangesAsync();

        Assert.That((await new TranslationRepository(c).ListAllAsync()).Select(x => x.Code.Value), Is.EqualTo(new[] { "CRM0001", "CRM0002" }));
    }

    [Test]
    public async Task GetByModuleAndLanguageAsync_ReturnsMatchingModuleAndLanguage()
    {
        await using I18NDbContext c = await ContextWithLanguagesAsync();

        c.Translations.AddRange(RepositoryTestData.Translation("CRM0001"), RepositoryTestData.Translation("API0001"));
        await c.SaveChangesAsync();

        Assert.That((await new TranslationRepository(c).GetByModuleAndLanguageAsync("crm", RepositoryTestData.LanguageCode("es-ES"))).Select(x => x.Code.Value), Is.EqualTo(new[] { "CRM0001" }));
    }

    [Test]
    public async Task GetVariantsAsync_ReturnsAllLanguagesForCode()
    {
        await using I18NDbContext c = await ContextWithLanguagesAsync();

        c.Translations.AddRange(RepositoryTestData.Translation("CRM0001", "es-ES"), RepositoryTestData.Translation("CRM0001", "en-US", "Hello"));
        await c.SaveChangesAsync();

        Assert.That((await new TranslationRepository(c).GetVariantsAsync(RepositoryTestData.TranslationCode("CRM0001"))).Select(x => x.LanguageCode.Value), Is.EqualTo(new[] { "en-US", "es-ES" }));
    }

    [Test]
    public async Task GetPagedAsync_ReturnsPageAndTotalCount()
    {
        await using I18NDbContext c = await ContextWithLanguagesAsync();

        c.Translations.AddRange(RepositoryTestData.Translation("CRM0001"), RepositoryTestData.Translation("CRM0002"), RepositoryTestData.Translation("CRM0003"));
        await c.SaveChangesAsync();
        (IReadOnlyList<Translation> Items, long TotalCount) result = await new TranslationRepository(c).GetPagedAsync(2, 1, "CRM", "es-ES");

        Assert.That(result.TotalCount, Is.EqualTo(3));
        Assert.That(result.Items.Select(x => x.Code.Value), Is.EqualTo(new[] { "CRM0002" }));
    }

    [Test]
    public async Task ExistsAsync_ByCodeAndLanguage_ReturnsExpectedValue()
    {
        await using I18NDbContext c = await ContextWithLanguagesAsync();

        c.Translations.Add(RepositoryTestData.Translation());
        await c.SaveChangesAsync(); var r = new TranslationRepository(c);

        Assert.That(await r.ExistsAsync(RepositoryTestData.TranslationCode("CRM0001"), RepositoryTestData.LanguageCode("es-ES")), Is.True);
        Assert.That(await r.ExistsAsync(RepositoryTestData.TranslationCode("CRM0002"), RepositoryTestData.LanguageCode("es-ES")), Is.False);
    }

    [Test]
    public async Task ExistsAsync_ByIdentifier_ReturnsExpectedValue()
    {
        await using I18NDbContext c = await ContextWithLanguagesAsync();

        Translation e = RepositoryTestData.Translation();
        c.Translations.Add(e);
        await c.SaveChangesAsync();

        var r = new TranslationRepository(c);

        Assert.That(await r.ExistsAsync(e.Id.Value), Is.True);
        Assert.That(await r.ExistsAsync(Guid.CreateVersion7()), Is.False);
    }

    [Test]
    public async Task AddAsync_AddsTranslationToChangeTracker()
    {
        await using I18NDbContext c = await ContextWithLanguagesAsync();
        Translation e = RepositoryTestData.Translation();

        await new TranslationRepository(c).AddAsync(e);

        Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Added));
    }

    [Test]
    public async Task Update_MarksTranslationAsModified()
    {
        await using I18NDbContext c = await ContextWithLanguagesAsync();
        Translation e = RepositoryTestData.Translation();

        c.Translations.Attach(e);
        new TranslationRepository(c).Update(e);

        Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Modified));
    }

    [Test]
    public async Task Remove_MarksTranslationAsDeleted()
    {
        await using I18NDbContext c = await ContextWithLanguagesAsync();
        Translation e = RepositoryTestData.Translation();

        c.Translations.Attach(e); new TranslationRepository(c).Remove(e);

        Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Deleted));
    }
}
