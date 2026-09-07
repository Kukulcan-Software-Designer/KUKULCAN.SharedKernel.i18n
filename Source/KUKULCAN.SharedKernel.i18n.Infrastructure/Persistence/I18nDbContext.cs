using KUKULCAN.SharedKernel.Database;
using KUKULCAN.SharedKernel.Database.Configuration;
using Microsoft.EntityFrameworkCore;
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

    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Translation> Translations => Set<Translation>();
    public DbSet<LocaleConfiguration> LocaleConfigurations => Set<LocaleConfiguration>();
    public DbSet<CurrencyFormat> CurrencyFormats => Set<CurrencyFormat>();

    protected override void ConfigureProvider(DbContextOptionsBuilder optionsBuilder)
    {
        var databaseOptions = _databaseOptions.Value;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("i18n");
        base.OnModelCreating(modelBuilder);
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
