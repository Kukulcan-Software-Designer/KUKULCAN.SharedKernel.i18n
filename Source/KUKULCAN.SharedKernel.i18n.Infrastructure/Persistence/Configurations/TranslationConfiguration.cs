using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="Translation"/>.
/// Maps translation entries to the <c>i18n.Translations</c> table.
/// </summary>
public sealed class TranslationConfiguration : IEntityTypeConfiguration<Translation>
{
    /// <summary>
    /// Configures the entity type for <see cref="Translation"/>.
    /// </summary>
    /// <param name="builder">The builder to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<Translation> builder)
    {
        builder.ToTable("Translations");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever()
            .HasConversion(id => id.Value, value => new I18nEntityId(value));

        // TranslationCode value object → stored as VARCHAR(9) (e.g. "ATLAS0001")
        builder.Property(t => t.Code)
            .HasColumnName("Code")
            .HasMaxLength(9)
            .HasConversion(
                v => v.Value,
                v => TranslationCode.From(v).Value)  // Result.Value safe — DB values are pre-validated
            .IsRequired();

        // LanguageCode value object (SharedKernel) → stored as VARCHAR(10) (e.g. "es-ES")
        builder.Property(t => t.LanguageCode)
            .HasColumnName("LanguageCode")
            .HasMaxLength(10)
            .HasConversion(
                v => v.Value,
                v => LanguageCode.Create(v).Value)
            .IsRequired();

        builder.Property(t => t.Text).HasMaxLength(4000).IsRequired();
        builder.Property(t => t.Context).HasMaxLength(500);
        builder.Property(t => t.MaxLength);
        builder.Property(t => t.IsReviewed).IsRequired();
        builder.Property(x => x.CreatedOn).IsRequired();
        builder.Property(x => x.ModifiedOn);

        // Composite unique index: (Code + LanguageCode) is the natural business key
        builder.HasIndex(t => new { t.Code, t.LanguageCode })
            .IsUnique()
            .HasDatabaseName("UX_Translations_Code_Language");

        // Index for language queries.
        builder.HasIndex(t => t.LanguageCode)
            .HasDatabaseName("IX_Translations_LanguageCode");

        // LanguageCode is a value object in Translation while Language.Code is a string.
        // EF Core requires the CLR types of FK and principal-key properties to match, even
        // when both properties are persisted as strings through value converters. Therefore
        // an actual EF relationship cannot be declared here without changing the domain model.
        // The language association remains a domain-level invariant and is intentionally not
        // represented as a database foreign key.
    }
}
