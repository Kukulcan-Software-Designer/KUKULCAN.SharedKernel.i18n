using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.UnitTests.Persistence.Repositories;


[TestFixture]
public sealed class LanguageRepositoryTests
{
    [SetUp]
    public Task SetUp() => RepositoryTestDatabase.ResetAsync();

    [Test]
    public async Task GetByIdAsync_ReturnsMatchingLanguage()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();
        Language e = RepositoryTestData.Language();

        c.Languages.Add(e);
        await c.SaveChangesAsync();

        Assert.That((await new LanguageRepository(c).GetByIdAsync(e.Id.Value))?.Id.Value, Is.EqualTo(e.Id.Value));
    }

    [Test]
    public async Task GetByCodeAsync_ReturnsMatchingLanguage()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();
        Language e = RepositoryTestData.Language();

        c.Languages.Add(e);
        await c.SaveChangesAsync();

        Assert.That((await new LanguageRepository(c).GetByCodeAsync(e.Code))?.Id.Value, Is.EqualTo(e.Id.Value));
    }

    [Test]
    public async Task ListAllAsync_ReturnsLanguagesOrderedByName()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();

        c.Languages.AddRange(RepositoryTestData.Language(name: "Spanish"), RepositoryTestData.Language("en-US", "English"));
        await c.SaveChangesAsync();

        Assert.That((await new LanguageRepository(c).ListAllAsync()).Select(x => x.Name), Is.EqualTo(["English", "Spanish"
        ]));
    }

    [Test]
    public async Task GetAllActiveAsync_ReturnsOnlyActiveLanguages()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();
        Language active = RepositoryTestData.Language();
        Language inactive = RepositoryTestData.Language("en-US", "English");

        inactive.Deactivate();
        c.Languages.AddRange(active, inactive);
        await c.SaveChangesAsync();

        Assert.That((await new LanguageRepository(c).GetAllActiveAsync()).Select(x => x.Code), Is.EqualTo(new[] { active.Code }));
    }

    [Test]
    public async Task GetDefaultAsync_ReturnsDefaultLanguage()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();
        Language e = RepositoryTestData.Language(isDefault: true);

        c.Languages.Add(e);
        await c.SaveChangesAsync();

        Assert.That((await new LanguageRepository(c).GetDefaultAsync())?.Id.Value, Is.EqualTo(e.Id.Value));
    }

    [Test]
    public async Task ExistsAsync_ReturnsExpectedValue()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();
        Language e = RepositoryTestData.Language();

        c.Languages.Add(e);
        await c.SaveChangesAsync();

        var r = new LanguageRepository(c);

        Assert.That(await r.ExistsAsync(e.Id.Value), Is.True);
        Assert.That(await r.ExistsAsync(Guid.CreateVersion7()), Is.False);
    }

    [Test]
    public async Task ExistsByCodeAsync_ReturnsExpectedValue()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();

        c.Languages.Add(RepositoryTestData.Language());
        await c.SaveChangesAsync();

        var r = new LanguageRepository(c);

        Assert.That(await r.ExistsByCodeAsync("es-ES"), Is.True);
        Assert.That(await r.ExistsByCodeAsync("fr-FR"), Is.False);
    }

    [Test]
    public async Task AddAsync_AddsLanguageToChangeTracker()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();
        Language e = RepositoryTestData.Language();

        await new LanguageRepository(c).AddAsync(e);

        Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Added));
    }

    [Test]
    public async Task Update_MarksLanguageAsModified()
    {
        await using I18NDbContext c = await RepositoryTestDatabase.CreateContextAsync();
        Language e = RepositoryTestData.Language();

        c.Languages.Attach(e);
        new LanguageRepository(c).Update(e);

        Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Modified));
    }
}
