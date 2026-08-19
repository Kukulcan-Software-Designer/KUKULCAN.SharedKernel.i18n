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
///   <item>Global soft-delete query filter for <c>soft-delete capable entities</c> entities.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Multi-tenancy note:</b> i18n data (languages, translations, locale configs,
/// currency formats) is <b>global</b> — shared across all tenants.
/// Therefore <c>tenant-aware properties</c> is not implemented by any i18n entity and the
/// tenant filter is never applied. The base class handles this correctly because
/// <c>ApplyTenantFilter</c> only filters entities that implement <c>tenant-aware properties</c>.
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

    // ── DbSets ────────────────────────────────────────────────────────────────
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

    // ── Model configuration ───────────────────────────────────────────────────
    /// <summary>
    /// Executes OnModelCreating.
    /// </summary>
    /// <param name="modelBuilder">The modelBuilder parameter.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Set default schema for all i18n tables
        modelBuilder.HasDefaultSchema("i18n");

        // Base class applies:
        //   - ApplyConfigurationsFromAssembly(GetType().Assembly)  → picks up all IEntityTypeConfiguration<T>
        //   - ApplySoftDeleteFilter()                              → WHERE IsDeleted = 0 (no i18n entities are soft-deletable, no-op)
        //   - ApplyTenantFilter(_tenantContext)                    → WHERE TenantId = @current (no i18n entities are ITenantAware, no-op)
        base.OnModelCreating(modelBuilder);
    }
}
