using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.UnitTests.Persistence.Configurations;

[TestFixture]
public sealed class EntityConfigurationTests
{
    [Test]
    public void CurrencyFormat_MapsExpectedTableAndKey()
    {
        var modelBuilder = new ModelBuilder();

        new CurrencyFormatConfiguration().Configure(modelBuilder.Entity<CurrencyFormat>());

        IMutableEntityType entity = modelBuilder.Model.FindEntityType(typeof(CurrencyFormat))!;

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

        IMutableEntityType entity = modelBuilder.Model.FindEntityType(typeof(Language))!;

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
    public void Language_DefaultIndex_IsUniqueAndFilteredToDefaultLanguages()
    {
        var modelBuilder = new ModelBuilder();

        new LanguageConfiguration().Configure(modelBuilder.Entity<Language>());

        IMutableEntityType entity = modelBuilder.Model.FindEntityType(typeof(Language))!;
        IMutableIndex index = entity.GetIndexes()
            .Single(index => index.Properties.Select(property => property.Name).SequenceEqual(new[] { nameof(Language.IsDefault) }));

        Assert.Multiple(() =>
        {
            Assert.That(index.IsUnique, Is.True);
            Assert.That(index.GetDatabaseName(), Is.EqualTo("UX_Languages_Default"));
            Assert.That(index.GetFilter(), Is.EqualTo("\"IsDefault\" = true"));
        });
    }

    [Test]
    public void Language_Relationships_AreNormalRelationshipsWithCascadeDelete()
    {
        var modelBuilder = new ModelBuilder();

        new LanguageConfiguration().Configure(modelBuilder.Entity<Language>());

        IMutableEntityType language = modelBuilder.Model.FindEntityType(typeof(Language))!;
        IMutableForeignKey localeForeignKey = modelBuilder.Model.FindEntityType(typeof(LocaleConfiguration))!
            .GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Language));
        IMutableForeignKey currencyForeignKey = modelBuilder.Model.FindEntityType(typeof(CurrencyFormat))!
            .GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Language));

        Assert.Multiple(() =>
        {
            Assert.That(language.FindNavigation(nameof(Language.LocaleConfiguration)), Is.Not.Null);
            Assert.That(language.FindNavigation(nameof(Language.CurrencyFormats)), Is.Not.Null);
            Assert.That(localeForeignKey.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade));
            Assert.That(currencyForeignKey.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade));
        });
    }

    [Test]
    public void LocaleConfiguration_MapsExpectedTableAndRequiredProperties()
    {
        var modelBuilder = new ModelBuilder();

        new LocaleConfigurationConfiguration().Configure(modelBuilder.Entity<LocaleConfiguration>());

        IMutableEntityType entity = modelBuilder.Model.FindEntityType(typeof(LocaleConfiguration))!;

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

        IMutableEntityType entity = modelBuilder.Model.FindEntityType(typeof(Translation))!;

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
