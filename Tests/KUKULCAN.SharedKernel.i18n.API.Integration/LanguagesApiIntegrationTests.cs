using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KUKULCAN.SharedKernel.i18n.Domain.DTOs;
using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.i18n.API.Integration;

[TestFixture]
public sealed class LanguagesApiIntegrationTests
{
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUpAsync()
    {
        await ApiIntegrationTestHost.ResetDatabaseAsync();
        _client = ApiIntegrationTestHost.Factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthenticationHandler.SchemeName);
    }

    [TearDown]
    public void TearDown() => _client.Dispose();

    [Test]
    public async Task GetLiveHealth_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/health/live");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetReadyHealth_UsesRealPostgreSql_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/health/ready");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetLanguage_ReadsDataFromRealDatabase()
    {
        await using var scope = ApiIntegrationTestHost.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<I18NDbContext>();
        context.Languages.Add(Language.Create(
            Guid.CreateVersion7(), "pt-PT", "Portuguese", "Português").Value);
        await context.SaveChangesAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/languages/pt-PT");
        LanguageDto? dto = await response.Content.ReadFromJsonAsync<LanguageDto>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.Code, Is.EqualTo("pt-PT"));
        Assert.That(dto.Name, Is.EqualTo("Portuguese"));
    }

    [Test]
    public async Task CreateLanguage_WritesThroughApplicationAndPersistsToRealDatabase()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/v1/languages",
            new
            {
                code = "nl-NL",
                name = "Dutch",
                nativeName = "Nederlands",
                isDefault = false,
            });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        using IServiceScope scope = ApiIntegrationTestHost.Factory.Services.CreateScope();
        I18NDbContext context = scope.ServiceProvider.GetRequiredService<I18NDbContext>();
        Language? language = await context.Languages.SingleOrDefaultAsync(x => x.Code == "nl-NL");

        Assert.That(language, Is.Not.Null);
        Assert.That(language!.Name, Is.EqualTo("Dutch"));
        Assert.That(language.NativeName, Is.EqualTo("Nederlands"));
    }
}
