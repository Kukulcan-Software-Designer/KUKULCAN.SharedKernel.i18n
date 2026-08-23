using KUKULCAN.SharedKernel.Database.Configuration;
using KUKULCAN.SharedKernel.i18n.Infrastructure;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence;

/// <summary>
/// Creates <see cref="I18NDbContext"/> for EF Core design-time operations.
/// </summary>
/// <remarks>
/// The API startup intentionally requires the production database connection string
/// to be supplied outside source control. EF Core tooling therefore creates the
/// context directly through the infrastructure registration and reads the connection
/// string from the <c>KUKULCAN__DATABASE__CONNECTIONSTRING</c> environment variable.
/// The i18n module uses PostgreSQL, so the design-time configuration explicitly selects
/// <see cref="DatabaseProvider.PostgresSql"/> instead of relying on the database
/// library default (SQL Server).
/// </remarks>
public sealed class I18NDbContextDesignTimeFactory : IDesignTimeDbContextFactory<I18NDbContext>
{
    /// <summary>
    /// Creates the i18n database context used by EF Core tooling.
    /// </summary>
    /// <param name="args">Design-time arguments supplied by EF Core.</param>
    /// <returns>A configured <see cref="I18NDbContext"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the design-time database connection string has not been configured.
    /// </exception>
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
        configuration["Kukulcan:Database:Provider"] = nameof(DatabaseProvider.PostgresSql);
        configuration["Kukulcan:Database:ConnectionString"] = connectionString;

        ServiceProvider serviceProvider = new ServiceCollection()
            .AddKukulcanI18NInfrastructure(configuration)
            .BuildServiceProvider();

        return serviceProvider.GetRequiredService<I18NDbContext>();
    }
}
