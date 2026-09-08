using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the KUKULCAN.SharedKernel.i18n module.
/// </summary>
public sealed class I18NDbContext(
    IOptions<KukulcanDatabaseOptions> options,
    ITenantContext tenantContext,
    IClock clock,
    IDomainEventDispatcher domainEventDispatcher)
    : KukulcanDbContextBase(options, tenantContext, clock, domainEventDispatcher)
{
    private readonly IOptions<KukulcanDatabaseOptions> _databaseOptions = options;

    /// <summary>
    /// Gets the language entities managed by the context.
    /// </summary>
    public DbSet<Language> Languages => Set<Language>();

    /// <summary>
    /// Gets the translation entities managed by the context.
    /// </summary>
    public DbSet<Translation> Translations => Set<Translation>();

    /// <summary>
    /// Gets the locale configuration entities managed by the context.
    /// </summary>
    public DbSet<LocaleConfiguration> LocaleConfigurations => Set<LocaleConfiguration>();

    /// <summary>
    /// Gets the currency format entities managed by the context.
    /// </summary>
    public DbSet<CurrencyFormat> CurrencyFormats => Set<CurrencyFormat>();

    /// <summary>
    /// Configures the EF Core database provider and its provider-specific options.
    /// </summary>
    /// <param name="optionsBuilder">The options builder used to configure the database context.</param>
    protected override void ConfigureProvider(DbContextOptionsBuilder optionsBuilder)
    {
        KukulcanDatabaseOptions databaseOptions = _databaseOptions.Value;

        if (databaseOptions.Provider == DatabaseProvider.PostgresSql)
        {
            optionsBuilder.UseNpgsql(
                databaseOptions.ConnectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(databaseOptions.CommandTimeoutSeconds);

                    if (databaseOptions.Retry.Enabled)
                    {
                        npgsqlOptions.EnableRetryOnFailure(
                            databaseOptions.Retry.MaxRetryCount,
                            TimeSpan.FromSeconds(databaseOptions.Retry.MaxRetryDelaySeconds),
                            errorCodesToAdd: null);
                    }
                });
        }
        else
        {
            base.ConfigureProvider(optionsBuilder);
        }

        ConfigureMigrationsAssembly(optionsBuilder, databaseOptions.Provider);
    }

    /// <summary>
    /// Configures the entity model and applies the provider-specific i18n schema strategy.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure the entity model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        DatabaseProvider provider = _databaseOptions.Value.Provider;

        // PostgreSQL and SQL Server support a schema namespace within the Atlas database.
        // MySQL treats schema as an alias for database, so applying "i18n" here would
        // redirect the model to a different database instead of the configured Atlas database.
        if (provider != DatabaseProvider.MySql)
        {
            modelBuilder.HasDefaultSchema("i18n");
        }

        base.OnModelCreating(modelBuilder);
        ConfigureDefaultLanguageInvariant(modelBuilder, provider);
    }

    /// <summary>
    /// Configures the database-specific representation of the invariant that
    /// at most one language can be marked as the default language.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure the entity model.</param>
    /// <param name="provider">The configured database provider.</param>
    private static void ConfigureDefaultLanguageInvariant(
        ModelBuilder modelBuilder,
        DatabaseProvider provider)
    {
        IndexBuilder indexBuilder;

        switch (provider)
        {
            case DatabaseProvider.PostgresSql:
                indexBuilder = modelBuilder.Entity<Language>()
                    .HasIndex(l => l.IsDefault)
                    .IsUnique()
                    .HasDatabaseName("UX_Languages_Default");
                indexBuilder.HasFilter("\"IsDefault\" = true");
                break;

            case DatabaseProvider.SqlServer:
                indexBuilder = modelBuilder.Entity<Language>()
                    .HasIndex(l => l.IsDefault)
                    .IsUnique()
                    .HasDatabaseName("UX_Languages_Default");
                indexBuilder.HasFilter("[IsDefault] = 1");
                break;

            case DatabaseProvider.MySql:
                // MySQL does not implement SQL Server/PostgreSQL-style filtered
                // indexes. A nullable generated marker produces the same invariant:
                // TRUE -> 1, FALSE -> NULL, and MySQL UNIQUE indexes permit multiple
                // NULL values while allowing only one 1.
                modelBuilder.Entity<Language>()
                    .Property<int?>("DefaultLanguageMarker")
                    .HasColumnName("DefaultLanguageMarker")
                    .HasComputedColumnSql(
                        "CASE WHEN `IsDefault` = 1 THEN 1 ELSE NULL END");

                modelBuilder.Entity<Language>()
                    .HasIndex("DefaultLanguageMarker")
                    .IsUnique()
                    .HasDatabaseName("UX_Languages_Default");
                break;

            default:
                throw new InvalidOperationException(
                    $"Database provider '{provider}' is not supported by the i18n default language configuration.");
        }
    }

    private static void ConfigureMigrationsAssembly(
        DbContextOptionsBuilder optionsBuilder,
        DatabaseProvider provider)
    {
        string assemblyName = provider switch
        {
            DatabaseProvider.PostgresSql => "KUKULCAN.SharedKernel.i18n.Migrations.PostgreSql",
            DatabaseProvider.SqlServer => "KUKULCAN.SharedKernel.i18n.Migrations.SqlServer",
            DatabaseProvider.MySql => "KUKULCAN.SharedKernel.i18n.Migrations.MySql",
            _ => throw new InvalidOperationException(
                $"Database provider '{provider}' is not supported by the i18n migrations configuration.")
        };

        RelationalOptionsExtension relationalOptions = optionsBuilder.Options.Extensions
            .OfType<RelationalOptionsExtension>()
            .SingleOrDefault()
            ?? throw new InvalidOperationException(
                "The i18n database provider did not register EF Core relational options.");

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder)
            .AddOrUpdateExtension(relationalOptions.WithMigrationsAssembly(assemblyName));
    }
}