using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.UnitTests.Persistence.Repositories;

[TestFixture]
public sealed class CurrencyFormatRepositoryTests
{
    [SetUp]
    public Task SetUp() => RepositoryTestDatabase.ResetAsync();

    [Test]
    public async Task GetByIdAsync_ReturnsMatchingFormat()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();
        CurrencyFormat e = RepositoryTestData.Currency();

        c.CurrencyFormats.Add(e);
        await c.SaveChangesAsync();

        Assert.That((await new CurrencyFormatRepository(c).GetByIdAsync(e.Id.Value))?.Id.Value, Is.EqualTo(e.Id.Value));
    }

    [Test]
    public async Task FindAsync_ReturnsMatchingFormatIgnoringCurrencyCase()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();
        CurrencyFormat e = RepositoryTestData.Currency();

        c.CurrencyFormats.Add(e);
        await c.SaveChangesAsync();

        Assert.That((await new CurrencyFormatRepository(c).FindAsync(RepositoryTestData.LanguageCode("es-ES"), "eur"))?.Id.Value, Is.EqualTo(e.Id.Value));
    }

    [Test]
    public async Task ListAllAsync_ReturnsFormatsOrderedByLanguageAndCurrency()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();

        c.CurrencyFormats.AddRange(RepositoryTestData.Currency("es-ES", "USD"), RepositoryTestData.Currency("en-US"));
        await c.SaveChangesAsync();

        Assert.That((await new CurrencyFormatRepository(c).ListAllAsync()).Select(x => $"{x.LanguageCode.Value}:{x.CurrencyCode}"), Is.EqualTo(
            ["en-US:EUR", "es-ES:USD"]));
    }

    [Test]
    public async Task GetByLanguageAsync_ReturnsOnlyRequestedLanguage()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();

        c.CurrencyFormats.AddRange(RepositoryTestData.Currency(), RepositoryTestData.Currency("en-US"));
        await c.SaveChangesAsync();

        Assert.That((await new CurrencyFormatRepository(c).GetByLanguageAsync(RepositoryTestData.LanguageCode("es-ES"))).Select(x => x.LanguageCode.Value), Is.EqualTo(
            ["es-ES"]));
    }

    [Test]
    public async Task ExistsAsync_ReturnsExpectedValue()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();
        CurrencyFormat e = RepositoryTestData.Currency();

        c.CurrencyFormats.Add(e);
        await c.SaveChangesAsync();

        var r = new CurrencyFormatRepository(c);

        Assert.That(await r.ExistsAsync(e.Id.Value), Is.True);
        Assert.That(await r.ExistsAsync(Guid.CreateVersion7()), Is.False);
    }

    [Test]
    public async Task AddAsync_AddsFormatToChangeTracker()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();
        CurrencyFormat e = RepositoryTestData.Currency();

        await new CurrencyFormatRepository(c).AddAsync(e);

        Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Added));
    }

    [Test]
    public async Task Update_MarksFormatAsModified()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();
        CurrencyFormat e = RepositoryTestData.Currency();

        c.CurrencyFormats.Attach(e); new CurrencyFormatRepository(c).Update(e);

        Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Modified));
    }

    [Test]
    public async Task Remove_MarksFormatAsDeleted()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();
        CurrencyFormat e = RepositoryTestData.Currency(); c.CurrencyFormats.Attach(e);

        new CurrencyFormatRepository(c).Remove(e);

        Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Deleted));
    }
}
