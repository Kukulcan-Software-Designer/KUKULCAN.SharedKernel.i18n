using KUKULCAN.SharedKernel.i18n.API.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.i18n.API.UnitTests.Extensions;

[TestFixture]
public sealed class ApiServiceExtensionsSecurityTests
{
    [Test]
    public void AddKukulcanI18NApi_WhenJwtSecretIsMissing_ThrowsConfigurationError()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "ITZAMNA",
                ["Jwt:Audience"] = "ITZAMNA.i18n",
            })
            .Build();

        IServiceCollection services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddKukulcanI18NApi(configuration));

        Assert.That(exception!.Message, Does.Contain("Jwt:SecretKey must be configured"));
    }

    [Test]
    public void AddKukulcanI18NApi_WhenJwtSecretIsTooShort_ThrowsConfigurationError()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "short-secret",
            })
            .Build();

        IServiceCollection services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddKukulcanI18NApi(configuration));

        Assert.That(exception!.Message, Does.Contain("at least 32 characters"));
    }

    [Test]
    public void AddKukulcanI18NApi_WhenJwtSecretIsValid_RegistersAuthenticationAndAuthorization()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "01234567890123456789012345678901",
                ["Jwt:Issuer"] = "ITZAMNA",
                ["Jwt:Audience"] = "ITZAMNA.i18n",
            })
            .Build();

        IServiceCollection services = new ServiceCollection();

        Assert.DoesNotThrow(() => services.AddKukulcanI18NApi(configuration));

        using ServiceProvider provider = services.BuildServiceProvider();
        AuthorizationOptions options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        AuthorizationPolicy? readPolicy = options.GetPolicy("i18n.read");
        AuthorizationPolicy? writePolicy = options.GetPolicy("i18n.write");

        Assert.That(readPolicy, Is.Not.Null);
        Assert.That(readPolicy!.Requirements, Has.Exactly(1).TypeOf<ClaimsAuthorizationRequirement>());
        Assert.That(writePolicy, Is.Not.Null);
        Assert.That(writePolicy!.Requirements, Has.Exactly(1).TypeOf<RolesAuthorizationRequirement>());
        Assert.That(
            ((RolesAuthorizationRequirement)writePolicy.Requirements.Single()).AllowedRoles,
            Is.EquivalentTo(new[] { "KUKULCAN.Admin", "KUKULCAN.i18n.Admin" }));
    }
}
