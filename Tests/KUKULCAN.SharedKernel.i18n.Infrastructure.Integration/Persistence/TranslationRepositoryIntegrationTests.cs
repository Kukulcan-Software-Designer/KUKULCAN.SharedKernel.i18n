using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence.Repositories;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.Integration.Persistence;

[TestFixture]
public sealed class TranslationRepositoryIntegrationTests
{
    [SetUp]
    public Task SetUp() => IntegrationTestDatabase.ResetAsync();

    private static async Task<I18NDbContext> CreateContextWithLanguagesAsync()
    {
        var context = await IntegrationTestDatabase.CreateContextAsync();
        context.Languages.AddRange(
            Language.Create(Guid.CreateVersion7(), "es-ES", "Spanish", "Español", true).Value,
            Language.Create(Guid.CreateVersion7(), "en-US", "English", "English", false).Value);
        await context.SaveChangesAsync();
        return context;
    }

    private static KUKULCAN.SharedKernel.i18n.Domain.ValueObjects.TranslationCode Code(string value)
        => KUKULCAN.SharedKernel.i18n.Domain.ValueObjects.TranslationCode.From(value).Value;

    private static KUKULCAN.SharedKernel.i18n.Domain.ValueObjects.LanguageCode LanguageCode(string value)
        => KUKULCAN.SharedKernel.i18n.Domain.ValueObjects.LanguageCode.Create(value).Value;

    [Test]
    public async Task Repository_PersistsAndQueriesTranslationsAgainstPostgreSql()
    {
        await using var context = await CreateContextWithLanguagesAsync();
        var repository = new TranslationRepository(context);
        var spanish = Translation.Create(Guid.CreateVersion7(), "CRM0001", "es-ES", "Hola").Value;
        var english = Translation.Create(Guid.CreateVersion7(), "CRM0001", "en-US", "Hello").Value;
        var secondModule = Translation.Create(Guid.CreateVersion7(), "API0001", "es-ES", "API").Value;

        await repository.AddAsync(spanish);
        await repository.AddAsync(english);
        await repository.AddAsync(secondModule);
        await context.SaveChangesAsync();

        Assert.That((await repository.GetByIdAsync(spanish.Id.Value))?.Text, Is.EqualTo("Hola"));
        Assert.That((await repository.FindAsync(Code("CRM0001"), LanguageCode("en-US")))?.Text, Is.EqualTo("Hello"));
        Assert.That((await repository.ListAllAsync()).Select(x => x.Code.Value), Is.EqualTo(new[] { "API0001", "CRM0001", "CRM0001" }));
        Assert.That((await repository.GetByModuleAndLanguageAsync("crm", LanguageCode("es-ES"))).Select(x => x.Code.Value), Is.EqualTo(new[] { "CRM0001" }));
        Assert.That((await repository.GetVariantsAsync(Code("CRM0001"))).Select(x => x.LanguageCode.Value), Is.EqualTo(new[] { "en-US", "es-ES" }));
        Assert.That(await repository.ExistsAsync(Code("CRM0001"), LanguageCode("es-ES")), Is.True);
        Assert.That(await repository.ExistsAsync(Code("ZZZ0001"), LanguageCode("es-ES")), Is.False);
        Assert.That(await repository.ExistsAsync(spanish.Id.Value), Is.True);
    }

    [Test]
    public async Task Repository_GetPagedAsyncExecutesAgainstPostgreSql()
    {
        await using var context = await CreateContextWithLanguagesAsync();
        var repository = new TranslationRepository(context);
        await repository.AddAsync(Translation.Create(Guid.CreateVersion7(), "CRM0001", "es-ES", "Uno").Value);
        await repository.AddAsync(Translation.Create(Guid.CreateVersion7(), "CRM0002", "es-ES", "Dos").Value);
        await repository.AddAsync(Translation.Create(Guid.CreateVersion7(), "CRM0003", "es-ES", "Tres").Value);
        await context.SaveChangesAsync();

        var result = await repository.GetPagedAsync(2, 1, "CRM", "es-ES");

        Assert.That(result.TotalCount, Is.EqualTo(3));
        Assert.That(result.Items.Select(x => x.Code.Value), Is.EqualTo(new[] { "CRM0002" }));
    }

    [Test]
    public async Task Repository_RemoveIsPersistedByPostgreSql()
    {
        await using var context = await CreateContextWithLanguagesAsync();
        var repository = new TranslationRepository(context);
        var translation = Translation.Create(Guid.CreateVersion7(), "CRM0001", "es-ES", "Hola").Value;
        await repository.AddAsync(translation);
        await context.SaveChangesAsync();

        repository.Remove(translation);
        await context.SaveChangesAsync();

        await using var verification = await IntegrationTestDatabase.CreateContextAsync();
        Assert.That(await new TranslationRepository(verification).ExistsAsync(translation.Id.Value), Is.False);
    }
}
