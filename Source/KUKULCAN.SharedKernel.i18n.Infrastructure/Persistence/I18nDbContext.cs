using KUKULCAN.SharedKernel.Database;
using KUKULCAN.SharedKernel.Database.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the KUKULCAN.SharedKernel.i18n module.
///
/// <para>
/// Extends <see cref="KukulcanDbContextBase"/> from
/// <c>KUKULCAN.SharedKernel.Database</c>, which wires automatically:
/// <list type="bullet">
///   <item>Audit fields via <c>AuditSaveChangesInterceptor</c>.</item>
///   <item>Soft-delete conversion via <c>SoftDeleteInterceptor</c>.</item>
///   <item>Domain event dispatch via <c>DomainEventDispatchInterceptor</c>.</item>
///   <item>Immutable entity enforcement via <c>ImmutableEntityInterceptor</c>.</item>
///   <item>Slow query logging via <c>SlowQueryInterceptor</c>.</item>
///   <item>All <c>IEntityTypeConfiguration&lt;T&gt;</c> found in this assembly.</item>
///   <item>Global soft-delete query filter for soft-delete capable entities.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Multi-tenancy note:</b> i18n data (languages, translations, locale configs,
/// currency formats) is <b>global</b> — shared across all tenants.
/// Therefore no i18n entity exposes a tenant-aware property and the tenant filter is not applied.
/// </para>
///
/// <para>
/// <b>PostgreSQL provider note:</b> the currently consumed
/// <c>KUKULCAN.SharedKernel.Database</c> package contains a provider assembly-name typo
/// in its reflection-based PostgreSQL configuration. This module already owns the
/// PostgreSQL provider dependency, so it configures Npgsql directly here rather than
/// changing the shared database abstraction or relying on a provider-specific workaround
/// in the tests.
/// </para>
///
/// <para>
/// Schema: <c>i18n</c> — isolated within the shared ATLAS database (or a dedicated DB).
/// </para>
/// </summary>
/// <param name="options">Database options.</param>
/// <param name="tenantContext">Current tenant context.</param>
/// <param name="clock">Clock used by persistence interceptors.</param>
/// <param name="domainEventDispatcher">Domain event dispatcher.</param>
public sealed class I18NDbContext(
    IOptions<KukulcanDatabaseOptions> options,
    ITenantContext tenantContext,
    IClock clock,
    IDomainEventDispatcher domainEventDispatcher)
    : KukulcanDbContextBase(options, tenantContext, clock, domainEventDispatcher)
{
    private readonly IOptions<KukulcanDatabaseOptions> _databaseOptions = options;

    /// <summary>
    /// Executes this member.
    /// </summary>
    public DbSet<Language> Languages => Set<Language>();

    /// <summary>
    /// Executes this member.
    /// </summary>
    public DbSet<Translation> Translations => Set<Translation>();

    /// <summary>
    /// Executes this member.
    /// </summary>
    public DbSet<LocaleConfiguration> LocaleConfigurations => Set<LocaleConfiguration>();

    /// <summary>
    /// Executes this member.
    /// </summary>
    public DbSet<CurrencyFormat> CurrencyFormats => Set<CurrencyFormat>();

    /// <summary>
    /// Configures the PostgreSQL provider directly for this module.
    /// </summary>
    /// <param name="optionsBuilder">EF Core options builder.</param>
    protected override void ConfigureProvider(DbContextOptionsBuilder optionsBuilder)
    {
        var databaseOptions = _databaseOptions.Value;

        if (databaseOptions.Provider != DatabaseProvider.PostgresSql)
        {
            base.ConfigureProvider(optionsBuilder);
            return;
        }

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

    /// <summary>
    /// Executes OnModelCreating.
    /// </summary>
    /// <param name="modelBuilder">The modelBuilder parameter.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("i18n");
        base.OnModelCreating(modelBuilder);
    }
}
