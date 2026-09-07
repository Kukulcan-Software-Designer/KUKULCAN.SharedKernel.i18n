using KUKULCAN.SharedKernel.Database.Configuration;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence;
using KUKULCAN.SharedKernel.i18n.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KUKULCAN.SharedKernel.i18n.Migrations.PostgreSql;

public sealed class I18NDbContextDesignTimeFactory : IDesignTimeDbContextFactory<I18NDbContext>
{
    public I18NDbContext CreateDbContext(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        configuration["Kukulcan:Database:Provider"] = nameof(DatabaseProvider.PostgresSql);

        ServiceCollection services = new();
        services.AddKukulcanI18NInfrastructure(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<I18NDbContext>();
    }
}
