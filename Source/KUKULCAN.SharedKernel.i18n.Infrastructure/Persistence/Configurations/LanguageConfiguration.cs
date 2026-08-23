using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="Language"/>.
/// Maps the global language catalogue to the <c>i18n.Languages</c> table.
/// </summary>
public sealed class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    /// <summary>
    /// Configures the entity type for <see cref="Language"/>.
    /// </summary>
    /// <param name="builder">The builder to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("Languages");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever()
            .HasConversion(id => id.Value, value => new I18nEntityId(value));

        // BCP-47 code is the unique business key (e.g. "es-ES")
        builder.Property(l => l.Code)
            .HasMaxLength(10)
            .IsRequired();

        builder.HasIndex(l => l.Code)
            .IsUnique()
            .HasDatabaseName("UX_Languages_Code");

        builder.Property(l => l.Name).HasMaxLength(100).IsRequired();
        builder.Property(l => l.NativeName).HasMaxLength(100).IsRequired();
        builder.Property(l => l.IsDefault).IsRequired();

        // IsActive comes from SharedKernel lifecycle state
        builder.Property(l => l.IsActive).IsRequired();
        builder.Property(x => x.CreatedOn).IsRequired();
        builder.Property(x => x.ModifiedOn);

        // Partial unique index: the database permits at most one default language.
        builder.HasIndex(l => l.IsDefault)
            .IsUnique()
            .HasFilter("\"IsDefault\" = true")
            .HasDatabaseName("UX_Languages_Default");

        // Navigation — one-to-one LocaleConfiguration relationship.
        builder.HasOne(l => l.LocaleConfiguration)
            .WithOne()
            .HasForeignKey<LocaleConfiguration>("LanguageId")
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation — one-to-many CurrencyFormats relationship.
        builder.HasMany(l => l.CurrencyFormats)
            .WithOne()
            .HasForeignKey("LanguageId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
