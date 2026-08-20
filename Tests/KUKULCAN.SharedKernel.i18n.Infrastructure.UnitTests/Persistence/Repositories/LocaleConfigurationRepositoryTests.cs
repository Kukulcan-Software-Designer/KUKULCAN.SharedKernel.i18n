using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.UnitTests.Persistence.Repositories;

[TestFixture]
public sealed class LocaleConfigurationRepositoryTests
{
    [SetUp]
    public Task SetUp() => RepositoryTestDatabase.ResetAsync();

    [Test]
    public async Task GetByIdAsync_ReturnsMatchingConfiguration()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();
        LocaleConfiguration e = RepositoryTestData.Locale();

        c.LocaleConfigurations.Add(e);
        await c.SaveChangesAsync();

        Assert.That((await new LocaleConfigurationRepository(c).GetByIdAsync(e.Id.Value))?.Id.Value, Is.EqualTo(e.Id.Value));
    }

    [Test]
    public async Task GetByLanguageAsync_ReturnsMatchingConfiguration()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();
        LocaleConfiguration e = RepositoryTestData.Locale();

        c.LocaleConfigurations.Add(e);
        await c.SaveChangesAsync();

        Assert.That((await new LocaleConfigurationRepository(c).GetByLanguageAsync(RepositoryTestData.LanguageCode("es-ES")))?.Id.Value, Is.EqualTo(e.Id.Value));
    }

    [Test]
    public async Task ListAllAsync_ReturnsConfigurationsOrderedByLanguageCode()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();

        c.LocaleConfigurations.AddRange(RepositoryTestData.Locale(), RepositoryTestData.Locale("en-US"));
        await c.SaveChangesAsync();

        Assert.That((await new LocaleConfigurationRepository(c).ListAllAsync()).Select(x => x.LanguageCode.Value), Is.EqualTo(
            ["en-US", "es-ES"]));
    }

    [Test]
    public async Task GetAllAsync_ReturnsAllConfigurations()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();

        c.LocaleConfigurations.AddRange(RepositoryTestData.Locale(), RepositoryTestData.Locale("en-US"));
        await c.SaveChangesAsync();

        Assert.That(await new LocaleConfigurationRepository(c).GetAllAsync(), Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ExistsAsync_ReturnsExpectedValue()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();
        LocaleConfiguration e = RepositoryTestData.Locale();

        c.LocaleConfigurations.Add(e);
        await c.SaveChangesAsync();

        var r = new LocaleConfigurationRepository(c);

        Assert.That(await r.ExistsAsync(e.Id.Value), Is.True);
        Assert.That(await r.ExistsAsync(Guid.CreateVersion7()), Is.False);
    }

    [Test]
    public async Task AddAsync_AddsConfigurationToChangeTracker()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();
        LocaleConfiguration e = RepositoryTestData.Locale();

        await new LocaleConfigurationRepository(c).AddAsync(e);

        Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Added));
    }

    [Test]
    public async Task Update_MarksConfigurationAsModified()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();
        LocaleConfiguration e = RepositoryTestData.Locale();

        c.LocaleConfigurations.Attach(e);
        new LocaleConfigurationRepository(c).Update(e);

        Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Modified));
    }
}
