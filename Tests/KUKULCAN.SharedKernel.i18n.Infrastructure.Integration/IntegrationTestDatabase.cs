using KUKULCAN.SharedKernel.Abstractions;
using KUKULCAN.SharedKernel.Database.Abstractions;
using KUKULCAN.SharedKernel.Database.Configuration;
using KUKULCAN.SharedKernel.DomainEvents.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Testcontainers.PostgreSql;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.Integration;

[SetUpFixture]
public sealed class IntegrationTestDatabase
{
    private static PostgreSqlContainer? _container;

    public static string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("The integration test database has not been initialized.");

    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        _container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("i18n_integration_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _container.StartAsync();
    }

    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    public static async Task<I18NDbContext> CreateContextAsync()
    {
        var options = Options.Create(new KukulcanDatabaseOptions
        {
            Provider = DatabaseProvider.PostgresSql,
            ConnectionString = ConnectionString,
            Retry = new KukulcanDatabaseOptions.RetryOptions { Enabled = false },
            Pool = new KukulcanDatabaseOptions.PoolOptions { Enabled = false },
        });

        var tenantContext = new Mock<ITenantContext>();
        var clock = new Mock<IClock>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var context = new I18NDbContext(options, tenantContext.Object, clock.Object, dispatcher.Object);

        await context.Database.EnsureCreatedAsync();
        return context;
    }

    public static async Task ResetAsync()
    {
        await using var context = await CreateContextAsync();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }
}
