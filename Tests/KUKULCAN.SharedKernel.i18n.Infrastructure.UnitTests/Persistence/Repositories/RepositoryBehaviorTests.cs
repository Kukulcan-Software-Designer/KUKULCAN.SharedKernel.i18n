using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Domain.ValueObjects;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

[assembly: LevelOfParallelism(1)]

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.UnitTests.Persistence.Repositories;

internal static class RepositoryTestData
{
    public static Language Language(string code = "es-ES", string name = "Spanish", bool isDefault = false)
        => Language.Create(Guid.CreateVersion7(), code, name, name, isDefault).Value;

    public static LocaleConfiguration Locale(string languageCode = "es-ES")
        => LocaleConfiguration.Create(Guid.CreateVersion7(), languageCode, "dd/MM/yyyy", "dd/MM/yyyy", "HH:mm", "dd/MM/yyyy HH:mm", FirstDayOfWeek.Monday, ',', '.', 2, 2).Value;

    public static CurrencyFormat Currency(string languageCode = "es-ES", string code = "EUR")
        => CurrencyFormat.Create(Guid.CreateVersion7(), languageCode, code, "Euro", "€", CurrencySymbolPosition.After, true, ',', '.', 2, "-{amount} {symbol}").Value;

    public static Translation Translation(string code = "CRM0001", string languageCode = "es-ES", string text = "Hola")
        => Translation.Create(Guid.CreateVersion7(), code, languageCode, text).Value;

    public static LanguageCode LanguageCode(string value) => KUKULCAN.SharedKernel.i18n.Domain.ValueObjects.LanguageCode.Create(value).Value;
    public static TranslationCode TranslationCode(string value) => KUKULCAN.SharedKernel.i18n.Domain.ValueObjects.TranslationCode.From(value).Value;
}

[TestFixture]
public sealed class LanguageRepositoryTests
{
    [SetUp] public Task SetUp() => RepositoryTestDatabase.ResetAsync();
    [Test] public async Task GetByIdAsync_ReturnsMatchingLanguage() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); var e = RepositoryTestData.Language(); c.Languages.Add(e); await c.SaveChangesAsync(); Assert.That((await new LanguageRepository(c).GetByIdAsync(e.Id.Value))?.Id.Value, Is.EqualTo(e.Id.Value)); }
    [Test] public async Task GetByCodeAsync_ReturnsMatchingLanguage() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); var e = RepositoryTestData.Language(); c.Languages.Add(e); await c.SaveChangesAsync(); Assert.That((await new LanguageRepository(c).GetByCodeAsync(e.Code))?.Id.Value, Is.EqualTo(e.Id.Value)); }
    [Test] public async Task ListAllAsync_ReturnsLanguagesOrderedByName() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); c.Languages.AddRange(RepositoryTestData.Language(name: "Spanish"), RepositoryTestData.Language("en-US", "English")); await c.SaveChangesAsync(); Assert.That((await new LanguageRepository(c).ListAllAsync()).Select(x => x.Name), Is.EqualTo(new[] { "English", "Spanish" })); }
    [Test] public async Task GetAllActiveAsync_ReturnsOnlyActiveLanguages() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); var active = RepositoryTestData.Language(); var inactive = RepositoryTestData.Language("en-US", "English"); inactive.Deactivate(); c.Languages.AddRange(active, inactive); await c.SaveChangesAsync(); Assert.That((await new LanguageRepository(c).GetAllActiveAsync()).Select(x => x.Code), Is.EqualTo(new[] { active.Code })); }
    [Test] public async Task GetDefaultAsync_ReturnsDefaultLanguage() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); var e = RepositoryTestData.Language(isDefault: true); c.Languages.Add(e); await c.SaveChangesAsync(); Assert.That((await new LanguageRepository(c).GetDefaultAsync())?.Id.Value, Is.EqualTo(e.Id.Value)); }
    [Test] public async Task ExistsAsync_ReturnsExpectedValue() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); var e = RepositoryTestData.Language(); c.Languages.Add(e); await c.SaveChangesAsync(); var r = new LanguageRepository(c); Assert.That(await r.ExistsAsync(e.Id.Value), Is.True); Assert.That(await r.ExistsAsync(Guid.CreateVersion7()), Is.False); }
    [Test] public async Task ExistsByCodeAsync_ReturnsExpectedValue() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); c.Languages.Add(RepositoryTestData.Language()); await c.SaveChangesAsync(); var r = new LanguageRepository(c); Assert.That(await r.ExistsByCodeAsync("es-ES"), Is.True); Assert.That(await r.ExistsByCodeAsync("fr-FR"), Is.False); }
    [Test] public async Task AddAsync_AddsLanguageToChangeTracker() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); var e = RepositoryTestData.Language(); await new LanguageRepository(c).AddAsync(e); Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Added)); }
    [Test] public async Task Update_MarksLanguageAsModified() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); var e = RepositoryTestData.Language(); c.Languages.Attach(e); new LanguageRepository(c).Update(e); Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Modified)); }
}

[TestFixture]
public sealed class LocaleConfigurationRepositoryTests
{
    [SetUp] public Task SetUp() => RepositoryTestDatabase.ResetAsync();
    [Test] public async Task GetByIdAsync_ReturnsMatchingConfiguration() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); var e = RepositoryTestData.Locale(); c.LocaleConfigurations.Add(e); await c.SaveChangesAsync(); Assert.That((await new LocaleConfigurationRepository(c).GetByIdAsync(e.Id.Value))?.Id.Value, Is.EqualTo(e.Id.Value)); }
    [Test] public async Task GetByLanguageAsync_ReturnsMatchingConfiguration() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); var e = RepositoryTestData.Locale(); c.LocaleConfigurations.Add(e); await c.SaveChangesAsync(); Assert.That((await new LocaleConfigurationRepository(c).GetByLanguageAsync(RepositoryTestData.LanguageCode("es-ES")))?.Id.Value, Is.EqualTo(e.Id.Value)); }
    [Test] public async Task ListAllAsync_ReturnsConfigurationsOrderedByLanguageCode() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); c.LocaleConfigurations.AddRange(RepositoryTestData.Locale("es-ES"), RepositoryTestData.Locale("en-US")); await c.SaveChangesAsync(); Assert.That((await new LocaleConfigurationRepository(c).ListAllAsync()).Select(x => x.LanguageCode.Value), Is.EqualTo(new[] { "en-US", "es-ES" })); }
    [Test] public async Task GetAllAsync_ReturnsAllConfigurations() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); c.LocaleConfigurations.AddRange(RepositoryTestData.Locale("es-ES"), RepositoryTestData.Locale("en-US")); await c.SaveChangesAsync(); Assert.That(await new LocaleConfigurationRepository(c).GetAllAsync(), Has.Count.EqualTo(2)); }
    [Test] public async Task ExistsAsync_ReturnsExpectedValue() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); var e = RepositoryTestData.Locale(); c.LocaleConfigurations.Add(e); await c.SaveChangesAsync(); var r = new LocaleConfigurationRepository(c); Assert.That(await r.ExistsAsync(e.Id.Value), Is.True); Assert.That(await r.ExistsAsync(Guid.CreateVersion7()), Is.False); }
    [Test] public async Task AddAsync_AddsConfigurationToChangeTracker() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); var e = RepositoryTestData.Locale(); await new LocaleConfigurationRepository(c).AddAsync(e); Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Added)); }
    [Test] public async Task Update_MarksConfigurationAsModified() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); var e = RepositoryTestData.Locale(); c.LocaleConfigurations.Attach(e); new LocaleConfigurationRepository(c).Update(e); Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Modified)); }
}

[TestFixture]
public sealed class CurrencyFormatRepositoryTests
{
    [SetUp] public Task SetUp() => RepositoryTestDatabase.ResetAsync();
    [Test] public async Task GetByIdAsync_ReturnsMatchingFormat() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); var e = RepositoryTestData.Currency(); c.CurrencyFormats.Add(e); await c.SaveChangesAsync(); Assert.That((await new CurrencyFormatRepository(c).GetByIdAsync(e.Id.Value))?.Id.Value, Is.EqualTo(e.Id.Value)); }
    [Test] public async Task FindAsync_ReturnsMatchingFormatIgnoringCurrencyCase() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); var e = RepositoryTestData.Currency(); c.CurrencyFormats.Add(e); await c.SaveChangesAsync(); Assert.That((await new CurrencyFormatRepository(c).FindAsync(RepositoryTestData.LanguageCode("es-ES"), "eur"))?.Id.Value, Is.EqualTo(e.Id.Value)); }
    [Test] public async Task ListAllAsync_ReturnsFormatsOrderedByLanguageAndCurrency() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); c.CurrencyFormats.AddRange(RepositoryTestData.Currency("es-ES", "USD"), RepositoryTestData.Currency("en-US", "EUR")); await c.SaveChangesAsync(); Assert.That((await new CurrencyFormatRepository(c).ListAllAsync()).Select(x => $"{x.LanguageCode.Value}:{x.CurrencyCode}"), Is.EqualTo(new[] { "en-US:EUR", "es-ES:USD" })); }
    [Test] public async Task GetByLanguageAsync_ReturnsOnlyRequestedLanguage() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); c.CurrencyFormats.AddRange(RepositoryTestData.Currency("es-ES"), RepositoryTestData.Currency("en-US")); await c.SaveChangesAsync(); Assert.That((await new CurrencyFormatRepository(c).GetByLanguageAsync(RepositoryTestData.LanguageCode("es-ES"))).Select(x => x.LanguageCode.Value), Is.EqualTo(new[] { "es-ES" })); }
    [Test] public async Task ExistsAsync_ReturnsExpectedValue() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); var e = RepositoryTestData.Currency(); c.CurrencyFormats.Add(e); await c.SaveChangesAsync(); var r = new CurrencyFormatRepository(c); Assert.That(await r.ExistsAsync(e.Id.Value), Is.True); Assert.That(await r.ExistsAsync(Guid.CreateVersion7()), Is.False); }
    [Test] public async Task AddAsync_AddsFormatToChangeTracker() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); var e = RepositoryTestData.Currency(); await new CurrencyFormatRepository(c).AddAsync(e); Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Added)); }
    [Test] public async Task Update_MarksFormatAsModified() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); var e = RepositoryTestData.Currency(); c.CurrencyFormats.Attach(e); new CurrencyFormatRepository(c).Update(e); Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Modified)); }
    [Test] public async Task Remove_MarksFormatAsDeleted() { await using var c = await RepositoryTestDatabase.CreateContextAsync(); var e = RepositoryTestData.Currency(); c.CurrencyFormats.Attach(e); new CurrencyFormatRepository(c).Remove(e); Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Deleted)); }
}

[TestFixture]
public sealed class TranslationRepositoryTests
{
    [SetUp] public Task SetUp() => RepositoryTestDatabase.ResetAsync();
    private static async Task<I18NDbContext> ContextWithLanguagesAsync() { var c = await RepositoryTestDatabase.CreateContextAsync(); c.Languages.AddRange(RepositoryTestData.Language(), RepositoryTestData.Language("en-US", "English")); await c.SaveChangesAsync(); return c; }
    [Test] public async Task GetByIdAsync_ReturnsMatchingTranslation() { await using var c = await ContextWithLanguagesAsync(); var e = RepositoryTestData.Translation(); c.Translations.Add(e); await c.SaveChangesAsync(); Assert.That((await new TranslationRepository(c).GetByIdAsync(e.Id.Value))?.Id.Value, Is.EqualTo(e.Id.Value)); }
    [Test] public async Task FindAsync_ReturnsMatchingTranslationByCodeAndLanguage() { await using var c = await ContextWithLanguagesAsync(); var e = RepositoryTestData.Translation(); c.Translations.Add(e); await c.SaveChangesAsync(); Assert.That((await new TranslationRepository(c).FindAsync(RepositoryTestData.TranslationCode("CRM0001"), RepositoryTestData.LanguageCode("es-ES")))?.Id.Value, Is.EqualTo(e.Id.Value)); }
    [Test] public async Task ListAllAsync_ReturnsTranslationsOrderedByCodeAndLanguage() { await using var c = await ContextWithLanguagesAsync(); c.Translations.AddRange(RepositoryTestData.Translation("CRM0002", "es-ES"), RepositoryTestData.Translation("CRM0001", "en-US")); await c.SaveChangesAsync(); Assert.That((await new TranslationRepository(c).ListAllAsync()).Select(x => x.Code.Value), Is.EqualTo(new[] { "CRM0001", "CRM0002" })); }
    [Test] public async Task GetByModuleAndLanguageAsync_ReturnsMatchingModuleAndLanguage() { await using var c = await ContextWithLanguagesAsync(); c.Translations.AddRange(RepositoryTestData.Translation("CRM0001"), RepositoryTestData.Translation("API0001")); await c.SaveChangesAsync(); Assert.That((await new TranslationRepository(c).GetByModuleAndLanguageAsync("crm", RepositoryTestData.LanguageCode("es-ES"))).Select(x => x.Code.Value), Is.EqualTo(new[] { "CRM0001" })); }
    [Test] public async Task GetVariantsAsync_ReturnsAllLanguagesForCode() { await using var c = await ContextWithLanguagesAsync(); c.Translations.AddRange(RepositoryTestData.Translation("CRM0001", "es-ES"), RepositoryTestData.Translation("CRM0001", "en-US", "Hello")); await c.SaveChangesAsync(); Assert.That((await new TranslationRepository(c).GetVariantsAsync(RepositoryTestData.TranslationCode("CRM0001"))).Select(x => x.LanguageCode.Value), Is.EqualTo(new[] { "en-US", "es-ES" })); }
    [Test] public async Task GetPagedAsync_ReturnsPageAndTotalCount() { await using var c = await ContextWithLanguagesAsync(); c.Translations.AddRange(RepositoryTestData.Translation("CRM0001"), RepositoryTestData.Translation("CRM0002"), RepositoryTestData.Translation("CRM0003")); await c.SaveChangesAsync(); var result = await new TranslationRepository(c).GetPagedAsync(2, 1, "CRM", "es-ES"); Assert.That(result.TotalCount, Is.EqualTo(3)); Assert.That(result.Items.Select(x => x.Code.Value), Is.EqualTo(new[] { "CRM0002" })); }
    [Test] public async Task ExistsAsync_ByCodeAndLanguage_ReturnsExpectedValue() { await using var c = await ContextWithLanguagesAsync(); c.Translations.Add(RepositoryTestData.Translation()); await c.SaveChangesAsync(); var r = new TranslationRepository(c); Assert.That(await r.ExistsAsync(RepositoryTestData.TranslationCode("CRM0001"), RepositoryTestData.LanguageCode("es-ES")), Is.True); Assert.That(await r.ExistsAsync(RepositoryTestData.TranslationCode("CRM0002"), RepositoryTestData.LanguageCode("es-ES")), Is.False); }
    [Test] public async Task ExistsAsync_ByIdentifier_ReturnsExpectedValue() { await using var c = await ContextWithLanguagesAsync(); var e = RepositoryTestData.Translation(); c.Translations.Add(e); await c.SaveChangesAsync(); var r = new TranslationRepository(c); Assert.That(await r.ExistsAsync(e.Id.Value), Is.True); Assert.That(await r.ExistsAsync(Guid.CreateVersion7()), Is.False); }
    [Test] public async Task AddAsync_AddsTranslationToChangeTracker() { await using var c = await ContextWithLanguagesAsync(); var e = RepositoryTestData.Translation(); await new TranslationRepository(c).AddAsync(e); Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Added)); }
    [Test] public async Task Update_MarksTranslationAsModified() { await using var c = await ContextWithLanguagesAsync(); var e = RepositoryTestData.Translation(); c.Translations.Attach(e); new TranslationRepository(c).Update(e); Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Modified)); }
    [Test] public async Task Remove_MarksTranslationAsDeleted() { await using var c = await ContextWithLanguagesAsync(); var e = RepositoryTestData.Translation(); c.Translations.Attach(e); new TranslationRepository(c).Remove(e); Assert.That(c.Entry(e).State, Is.EqualTo(EntityState.Deleted)); }
}
