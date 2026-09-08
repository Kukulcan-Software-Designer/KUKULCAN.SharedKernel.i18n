using KUKULCAN.SharedKernel.Database.Configuration;
using KUKULCAN.SharedKernel.i18n.Infrastructure;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KUKULCAN.SharedKernel.i18n.Migrations.MySql;

/// <summary>
/// Creates <see cref="I18NDbContext"/> for MySQL EF Core design-time operations.
/// </summary>
public sealed class I18NDbContextDesignTimeFactory : IDesignTimeDbContextFactory<I18NDbContext>
{
    /// <inheritdoc />
    public I18NDbContext CreateDbContext(string[] args)
    {
        string? connectionString = Environment.GetEnvironmentVariable(
            "KUKULCAN__DATABASE__CONNECTIONSTRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "KUKULCAN__DATABASE__CONNECTIONSTRING must be configured for EF Core design-time operations.");
        }

        var configuration = new ConfigurationManager();
        configuration["Kukulcan:Database:Provider"] = nameof(DatabaseProvider.MySql);
        configuration["Kukulcan:Database:ConnectionString"] = connectionString;

        ServiceProvider serviceProvider = new ServiceCollection()
            .AddKukulcanI18NInfrastructure(configuration)
            .BuildServiceProvider();

        return serviceProvider.GetRequiredService<I18NDbContext>();
    }
}
