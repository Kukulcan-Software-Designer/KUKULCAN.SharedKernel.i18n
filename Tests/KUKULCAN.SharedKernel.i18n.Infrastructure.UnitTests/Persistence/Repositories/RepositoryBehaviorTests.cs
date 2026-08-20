using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Domain.ValueObjects;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.UnitTests.Persistence.Repositories;

[assembly: LevelOfParallelism(1)]

[TestFixture]
public sealed class LanguageRepositoryTests
{
    [SetUp] public Task SetUp() => RepositoryTestDatabase.ResetAsync();

    private static Language NewLanguage(string code = "es-ES", string name = "Spanish", bool isDefault = false)
        => Language.Create(Guid.CreateVersion7(), code, name, name, isDefault).Value;

    [Test] public async Task GetByIdAsync_ReturnsMatchingLanguage()
    {
        await using var context = await RepositoryTestDatabase.CreateContextAsync();
        var entity = NewLanguage(); context.Languages.Add(entity); await context.SaveChangesAsync();
        var result = await new LanguageRepository(context).GetByIdAsync(entity.Id.Value);
        Assert.That(result?.Id.Value, Is.EqualTo(entity.Id.Value));
    }

    [Test] public async Task GetByCodeAsync_ReturnsMatchingLanguage()
    {
        await using var context = await RepositoryTestDatabase.CreateContextAsync();
        var entity = NewLanguage(); context.Languages.Add(entity); await context.SaveChangesAsync();
        var result = await new LanguageRepository(context).GetByCodeAsync("es-ES");
        Assert.That(result?.Id.Value, Is.EqualTo(entity.Id.Value));
    }

    [Test] public async Task ListAllAsync_ReturnsLanguagesOrderedByName()
    {
        await using var context = await RepositoryTestDatabase.CreateContextAsync();
        context.Languages.AddRange(NewLanguage(name: "Spanish"), NewLanguage("en-US", "English")); await context.SaveChangesAsync();
        var result = await new LanguageRepository(context).ListAllAsync();
        Assert.That(result.Select(x => x.Name), Is.EqualTo(new[] { "English", "Spanish" }));
    }

    [Test] public async Task GetAllActiveAsync_ReturnsOnlyActiveLanguages()
    {
        await using var context = await RepositoryTestDatabase.CreateContextAsync();
        var active = NewLanguage(); var inactive = NewLanguage("en-US", "English"); inactive.Deactivate(); context.Languages.AddRange(active, inactive); await context.SaveChangesAsync();
        var result = await new LanguageRepository(context).GetAllActiveAsync();
        Assert.That(result.Select(x => x.Code), Is.EqualTo(new[] { "es-ES" }));
    }

    [Test] public async Task GetDefaultAsync_ReturnsDefaultLanguage()
    {
        await using var context = await RepositoryTestDatabase.CreateContextAsync();
        var entity = NewLanguage(isDefault: true); context.Languages.Add(entity); await context.SaveChangesAsync();
        Assert.That((await new LanguageRepository(context).GetDefaultAsync())?.Id.Value, Is.EqualTo(entity.Id.Value));
    }

    [Test] public async Task ExistsAsync_ReturnsExpectedValue()
    {
        await using var context = await RepositoryTestDatabase.CreateContextAsync(); var entity = NewLanguage(); context.Languages.Add(entity); await context.SaveChangesAsync(); var repository = new LanguageRepository(context);
        Assert.That(await repository.ExistsAsync(entity.Id.Value), Is.True); Assert.That(await repository.ExistsAsync(Guid.CreateVersion7()), Is.False);
    }

    [Test] public async Task ExistsByCodeAsync_ReturnsExpectedValue()
    {
        await using var context = await RepositoryTestDatabase.CreateContextAsync(); context.Languages.Add(NewLanguage()); await context.SaveChangesAsync(); var repository = new LanguageRepository(context);
        Assert.That(await repository.ExistsByCodeAsync("es-ES"), Is.True); Assert.That(await repository.ExistsByCodeAsync("fr-FR"), Is.False);
    }

    [Test] public async Task AddAsync_AddsLanguageToChangeTracker()
    {
        await using var context = await RepositoryTestDatabase.CreateContextAsync(); var entity = NewLanguage(); await new LanguageRepository(context).AddAsync(entity);
        Assert.That(context.Entry(entity).State, Is.EqualTo(EntityState.Added));
    }

    [Test] public async Task Update_MarksLanguageAsModified()
    {
        await using var context = await RepositoryTestDatabase.CreateContextAsync(); var entity = NewLanguage(); context.Languages.Attach(entity); new LanguageRepository(context).Update(entity);
        Assert.That(context.Entry(entity).State, Is.EqualTo(EntityState.Modified));
    }
}
