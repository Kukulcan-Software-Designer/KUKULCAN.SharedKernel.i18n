using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace KUKULCAN.SharedKernel.i18n.API.Integration;

[SetUpFixture]
public sealed class ApiIntegrationTestHost
{
    private const string IntegrationJwtSecret = "KUKULCAN_INTEGRATION_TEST_SECRET_KEY_MINIMUM_32_CHARS";
    private const string IntegrationJwtIssuer = "KUKULCAN.IntegrationTests";
    private const string IntegrationJwtAudience = "KUKULCAN.SharedKernel.i18n.IntegrationTests";

    private static PostgreSqlContainer? _postgresqlContainer;
    private static RedisContainer? _redisContainer;
    private static ApiWebApplicationFactory? _factory;

    public static ApiWebApplicationFactory Factory =>
        _factory ?? throw new InvalidOperationException("The API integration test host has not been initialized.");

    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        _postgresqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("i18n_api_integration_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await _postgresqlContainer.StartAsync();
        await _redisContainer.StartAsync();

        string postgresqlConnectionString = _postgresqlContainer.GetConnectionString();
        string redisConnectionString = _redisContainer.GetConnectionString();

        // Testcontainers exposes dynamically mapped host ports. The application
        // must receive those values before WebApplication is built so that both
        // EF Core and the readiness health checks use the same real containers.
        Environment.SetEnvironmentVariable(
            "Kukulcan__Database__ConnectionString",
            postgresqlConnectionString);
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Redis",
            redisConnectionString);

        // The API requires a JWT signing key during service registration. These
        // values exist only for the integration-test process and are never read
        // from production configuration or committed application settings.
        Environment.SetEnvironmentVariable("Jwt__SecretKey", IntegrationJwtSecret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", IntegrationJwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", IntegrationJwtAudience);

        _factory = new ApiWebApplicationFactory(
            postgresqlConnectionString,
            redisConnectionString);
    }

    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        _factory?.Dispose();

        Environment.SetEnvironmentVariable("Kukulcan__Database__ConnectionString", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", null);
        Environment.SetEnvironmentVariable("Jwt__SecretKey", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);

        if (_redisContainer is not null)
            await _redisContainer.DisposeAsync();

        if (_postgresqlContainer is not null)
            await _postgresqlContainer.DisposeAsync();
    }

    public static async Task ResetDatabaseAsync()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        I18NDbContext context = scope.ServiceProvider.GetRequiredService<I18NDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }
}

public sealed class ApiWebApplicationFactory(
    string postgresqlConnectionString,
    string redisConnectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = redisConnectionString,
                ["Kukulcan:Database:Provider"] = "PostgresSql",
                ["Kukulcan:Database:ConnectionString"] = postgresqlConnectionString,
                ["Kukulcan:Database:Retry:Enabled"] = "false",
                ["Kukulcan:Database:Pool:Enabled"] = "false",
                ["Kukulcan:Database:Migration:AutoMigrateOnStartup"] = "false",
                ["Jwt:SecretKey"] = IntegrationJwtSecret,
                ["Jwt:Issuer"] = IntegrationJwtIssuer,
                ["Jwt:Audience"] = IntegrationJwtAudience,
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.SchemeName, _ => { });
        });
    }
}
