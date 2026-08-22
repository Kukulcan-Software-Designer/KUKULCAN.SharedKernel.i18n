using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Domain.ValueObjects.Enums;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence.Repositories;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.Integration.Persistence;

[TestFixture]
public sealed class CurrencyFormatRepositoryIntegrationTests
{
    [SetUp]
    public Task SetUp() => IntegrationTestDatabase.ResetAsync();

    [Test]
    public async Task FindAsync_UsesPostgreSqlTranslationForCaseInsensitiveCurrencyCode()
    {
        await using var context = await IntegrationTestDatabase.CreateContextAsync();

        var created = CurrencyFormat.Create(
            Guid.CreateVersion7(),
            "es-ES",
            "EUR",
            "Euro",
            "€",
            CurrencySymbolPosition.After,
            true,
            ',',
            '.',
            2);

        Assert.That(created.IsSuccess, Is.True, created.IsFailure ? created.Error.ToString() : string.Empty);
        context.CurrencyFormats.Add(created.Value);
        await context.SaveChangesAsync();

        var repository = new CurrencyFormatRepository(context);
        var result = await repository.FindAsync(
            LanguageCode.Create("es-ES").Value,
            "eur");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.CurrencyCode, Is.EqualTo("EUR"));
        Assert.That(result.LanguageCode.Value, Is.EqualTo("es-ES"));
    }

    [Test]
    public async Task AddUpdateAndRemove_PersistChangesInPostgreSql()
    {
        await using var context = await IntegrationTestDatabase.CreateContextAsync();

        var created = CurrencyFormat.Create(
            Guid.CreateVersion7(),
            "en-US",
            "USD",
            "US Dollar",
            "$",
            CurrencySymbolPosition.Before,
            false,
            '.',
            ',',
            2);

        Assert.That(created.IsSuccess, Is.True, created.IsFailure ? created.Error.ToString() : string.Empty);
        var entity = created.Value;
        var repository = new CurrencyFormatRepository(context);

        await repository.AddAsync(entity);
        await context.SaveChangesAsync();

        Assert.That(await repository.ExistsAsync(entity.Id.Value), Is.True);

        var loaded = await repository.GetByIdAsync(entity.Id.Value);
        Assert.That(loaded, Is.Not.Null);

        loaded!.Update(
            "US Dollar Updated",
            "USD",
            CurrencySymbolPosition.Before,
            true,
            '.',
            ',',
            2,
            "-{symbol}{amount}");
        repository.Update(loaded);
        await context.SaveChangesAsync();

        var updated = await repository.GetByIdAsync(entity.Id.Value);
        Assert.That(updated!.CurrencyName, Is.EqualTo("US Dollar Updated"));

        repository.Remove(updated);
        await context.SaveChangesAsync();

        Assert.That(await repository.ExistsAsync(entity.Id.Value), Is.False);
    }
}
