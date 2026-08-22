using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence.Repositories;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.Integration.Persistence;

[TestFixture]
public sealed class LanguageRepositoryIntegrationTests
{
    [SetUp]
    public Task SetUp() => IntegrationTestDatabase.ResetAsync();

    [Test]
    public async Task Repository_PersistsAndQueriesLanguagesAgainstPostgreSql()
    {
        await using var context = await IntegrationTestDatabase.CreateContextAsync();
        var repository = new LanguageRepository(context);
        var spanish = Language.Create(Guid.CreateVersion7(), "es-ES", "Spanish", "Español", true).Value;
        var english = Language.Create(Guid.CreateVersion7(), "en-US", "English", "English", false).Value;

        await repository.AddAsync(spanish);
        await repository.AddAsync(english);
        await context.SaveChangesAsync();

        Assert.That((await repository.GetByIdAsync(spanish.Id.Value))?.Code, Is.EqualTo("es-ES"));
        Assert.That((await repository.GetByCodeAsync("en-US"))?.Id.Value, Is.EqualTo(english.Id.Value));
        Assert.That((await repository.ListAllAsync()).Select(x => x.Code), Is.EqualTo(new[] { "en-US", "es-ES" }));
        Assert.That((await repository.GetAllActiveAsync()).Select(x => x.Code), Is.EqualTo(new[] { "en-US", "es-ES" }));
        Assert.That((await repository.GetDefaultAsync())?.Code, Is.EqualTo("es-ES"));
        Assert.That(await repository.ExistsAsync(spanish.Id.Value), Is.True);
        Assert.That(await repository.ExistsByCodeAsync("es-ES"), Is.True);
    }

    [Test]
    public async Task Repository_UpdateIsPersistedByPostgreSql()
    {
        await using var context = await IntegrationTestDatabase.CreateContextAsync();
        var repository = new LanguageRepository(context);
        var language = Language.Create(Guid.CreateVersion7(), "es-ES", "Spanish", "Español", false).Value;
        await repository.AddAsync(language);
        await context.SaveChangesAsync();

        language.Activate();
        repository.Update(language);
        await context.SaveChangesAsync();

        await using var verification = await IntegrationTestDatabase.CreateContextAsync();
        var persisted = await new LanguageRepository(verification).GetByCodeAsync("es-ES");
        Assert.That(persisted?.IsActive, Is.True);
    }
}
