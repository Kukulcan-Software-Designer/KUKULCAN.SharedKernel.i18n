using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence.Configurations;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.UnitTests.Persistence.Configurations;

[TestFixture]
public sealed class EntityConfigurationTests
{
    [Test]
    public void CurrencyFormat_MapsExpectedTableAndKey()
    {
        var modelBuilder = new ModelBuilder();

        new CurrencyFormatConfiguration().Configure(modelBuilder.Entity<CurrencyFormat>());

        var entity = modelBuilder.Model.FindEntityType(typeof(CurrencyFormat))!;

        Assert.Multiple(() =>
        {
            Assert.That(entity.GetTableName(), Is.EqualTo("CurrencyFormats"));
            Assert.That(entity.FindPrimaryKey()!.Properties.Select(p => p.Name), Is.EqualTo(new[] { "Id" }));
            Assert.That(entity.FindProperty(nameof(CurrencyFormat.CurrencyCode))!.IsNullable, Is.False);
            Assert.That(entity.FindProperty(nameof(CurrencyFormat.CurrencyName))!.GetMaxLength(), Is.EqualTo(100));
            Assert.That(entity.FindProperty(nameof(CurrencyFormat.Symbol))!.GetMaxLength(), Is.EqualTo(5));
            Assert.That(entity.FindProperty(nameof(CurrencyFormat.NegativePattern))!.GetMaxLength(), Is.EqualTo(30));
        });
    }

    [Test]
    public void Language_MapsExpectedTableAndRequiredProperties()
    {
        var modelBuilder = new ModelBuilder();

        new LanguageConfiguration().Configure(modelBuilder.Entity<Language>());

        var entity = modelBuilder.Model.FindEntityType(typeof(Language))!;

        Assert.Multiple(() =>
        {
            Assert.That(entity.GetTableName(), Is.EqualTo("Languages"));
            Assert.That(entity.FindPrimaryKey()!.Properties.Select(p => p.Name), Is.EqualTo(new[] { "Id" }));
            Assert.That(entity.FindProperty(nameof(Language.Code))!.IsNullable, Is.False);
            Assert.That(entity.FindProperty(nameof(Language.Name))!.IsNullable, Is.False);
            Assert.That(entity.FindProperty(nameof(Language.NativeName))!.IsNullable, Is.False);
        });
    }

    [Test]
    public void LocaleConfiguration_MapsExpectedTableAndRequiredProperties()
    {
        var modelBuilder = new ModelBuilder();

        new LocaleConfigurationConfiguration().Configure(modelBuilder.Entity<LocaleConfiguration>());

        var entity = modelBuilder.Model.FindEntityType(typeof(LocaleConfiguration))!;

        Assert.Multiple(() =>
        {
            Assert.That(entity.GetTableName(), Is.EqualTo("LocaleConfigurations"));
            Assert.That(entity.FindPrimaryKey()!.Properties.Select(p => p.Name), Is.EqualTo(new[] { "Id" }));
            Assert.That(entity.FindProperty(nameof(LocaleConfiguration.LanguageCode))!.IsNullable, Is.False);
            Assert.That(entity.FindProperty(nameof(LocaleConfiguration.DateFormat))!.IsNullable, Is.False);
            Assert.That(entity.FindProperty(nameof(LocaleConfiguration.DecimalPlaces))!.IsNullable, Is.False);
            Assert.That(entity.FindProperty(nameof(LocaleConfiguration.CurrencyDecimalPlaces))!.IsNullable, Is.False);
        });
    }

    [Test]
    public void Translation_MapsExpectedTableAndRequiredProperties()
    {
        var modelBuilder = new ModelBuilder();

        new TranslationConfiguration().Configure(modelBuilder.Entity<Translation>());

        var entity = modelBuilder.Model.FindEntityType(typeof(Translation))!;

        Assert.Multiple(() =>
        {
            Assert.That(entity.GetTableName(), Is.EqualTo("Translations"));
            Assert.That(entity.FindPrimaryKey()!.Properties.Select(p => p.Name), Is.EqualTo(new[] { "Id" }));
            Assert.That(entity.FindProperty(nameof(Translation.Code))!.IsNullable, Is.False);
            Assert.That(entity.FindProperty(nameof(Translation.LanguageCode))!.IsNullable, Is.False);
            Assert.That(entity.FindProperty(nameof(Translation.Text))!.IsNullable, Is.False);
        });
    }
}
