using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace KUKULCAN.SharedKernel.i18n.API.Integration;

[SetUpFixture]
public sealed class ApiIntegrationTestHost
{
    private static PostgreSqlContainer? _container;
    private static ApiWebApplicationFactory? _factory;

    public static ApiWebApplicationFactory Factory =>
        _factory ?? throw new InvalidOperationException("The API integration test host has not been initialized.");

    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("i18n_api_integration_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _container.StartAsync();
        _factory = new ApiWebApplicationFactory(_container.GetConnectionString());
    }

    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        _factory?.Dispose();
        if (_container is not null)
            await _container.DisposeAsync();
    }

    public static async Task ResetDatabaseAsync()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        I18NDbContext context = scope.ServiceProvider.GetRequiredService<I18NDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }
}

public sealed class ApiWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = connectionString,
                ["ConnectionStrings:Redis"] = string.Empty,
                ["Kukulcan:Database:Provider"] = "PostgresSql",
                ["Kukulcan:Database:ConnectionString"] = connectionString,
                ["Kukulcan:Database:Retry:Enabled"] = "false",
                ["Kukulcan:Database:Pool:Enabled"] = "false",
                ["Database:AutoMigrate"] = "false",
                ["Jwt:SecretKey"] = "KUKULCAN_INTEGRATION_TEST_SECRET_KEY_MINIMUM_32_CHARS",
                ["Jwt:Issuer"] = "KUKULCAN.IntegrationTests",
                ["Jwt:Audience"] = "KUKULCAN.SharedKernel.i18n.IntegrationTests",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.Scheme;
                options.DefaultChallengeScheme = TestAuthenticationHandler.Scheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.Scheme, _ => { });
        });
    }
}
