using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Domain.ValueObjects.Enums;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence.Repositories;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.Integration.Persistence;

[TestFixture]
public sealed class LocaleConfigurationRepositoryIntegrationTests
{
    [SetUp]
    public Task SetUp() => IntegrationTestDatabase.ResetAsync();

    [Test]
    public async Task Repository_PersistsAndQueriesLocaleConfigurationsAgainstPostgreSql()
    {
        await using var context = await IntegrationTestDatabase.CreateContextAsync();
        var repository = new LocaleConfigurationRepository(context);
        var spanish = LocaleConfiguration.Create(Guid.CreateVersion7(), "es-ES", "dd/MM/yyyy", "dd/MM/yyyy", "HH:mm", "dd/MM/yyyy HH:mm", FirstDayOfWeek.Monday, ',', '.', 2, 2).Value;
        var english = LocaleConfiguration.Create(Guid.CreateVersion7(), "en-US", "MM/dd/yyyy", "MM/dd/yyyy", "HH:mm", "MM/dd/yyyy HH:mm", FirstDayOfWeek.Sunday, '.', ',', 2, 2).Value;

        await repository.AddAsync(spanish);
        await repository.AddAsync(english);
        await context.SaveChangesAsync();

        Assert.That((await repository.GetByIdAsync(spanish.Id.Value))?.LanguageCode.Value, Is.EqualTo("es-ES"));
        Assert.That((await repository.GetByLanguageAsync(RepositoryTestLanguageCode("en-US")))?.Id.Value, Is.EqualTo(english.Id.Value));
        Assert.That((await repository.ListAllAsync()).Select(x => x.LanguageCode.Value), Is.EqualTo(new[] { "en-US", "es-ES" }));
        Assert.That(await repository.GetAllAsync(), Has.Count.EqualTo(2));
        Assert.That(await repository.ExistsAsync(spanish.Id.Value), Is.True);
        Assert.That(await repository.ExistsAsync(Guid.CreateVersion7()), Is.False);
    }

    [Test]
    public async Task Repository_UpdateIsPersistedByPostgreSql()
    {
        await using var context = await IntegrationTestDatabase.CreateContextAsync();
        var repository = new LocaleConfigurationRepository(context);
        var configuration = LocaleConfiguration.Create(Guid.CreateVersion7(), "es-ES", "dd/MM/yyyy", "dd/MM/yyyy", "HH:mm", "dd/MM/yyyy HH:mm", FirstDayOfWeek.Monday, ',', '.', 2, 2).Value;
        await repository.AddAsync(configuration);
        await context.SaveChangesAsync();

        repository.Update(configuration);
        await context.SaveChangesAsync();

        await using var verification = await IntegrationTestDatabase.CreateContextAsync();
        Assert.That((await new LocaleConfigurationRepository(verification).GetByLanguageAsync(RepositoryTestLanguageCode("es-ES")))?.Id.Value, Is.EqualTo(configuration.Id.Value));
    }

    private static KUKULCAN.SharedKernel.i18n.Domain.ValueObjects.LanguageCode RepositoryTestLanguageCode(string value)
        => KUKULCAN.SharedKernel.i18n.Domain.ValueObjects.LanguageCode.Create(value).Value;
}
