using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KUKULCAN.SharedKernel.i18n.Domain.DTOs;
using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
        const int maxAttempts = 10;
        const int retryDelayMilliseconds = 250;

        var statusCode = HttpStatusCode.ServiceUnavailable;
        string responseBody = string.Empty;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using HttpResponseMessage response = await _client.GetAsync("/health/ready");
            statusCode = response.StatusCode;

            if (response.IsSuccessStatusCode)
                return;

            responseBody = await response.Content.ReadAsStringAsync();

            if (attempt < maxAttempts)
                await Task.Delay(retryDelayMilliseconds);
        }

        string healthDetails = await GetReadyHealthDetailsAsync();

        Assert.That(
            statusCode,
            Is.EqualTo(HttpStatusCode.OK),
            $"The PostgreSQL/Redis readiness check did not become healthy after {maxAttempts} attempts. " +
            $"Endpoint response: {responseBody}. Health checks: {healthDetails}");
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
    public async Task GetLanguage_WhenLanguageDoesNotExist_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/v1/languages/xx-XX");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task CreateLanguage_WhenNameIsEmpty_ReturnsUnprocessableEntity()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/v1/languages",
            new
            {
                code = "fr-FR",
                name = "",
                nativeName = "Français",
                isDefault = false,
            });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
    }

    [Test]
    public async Task CreateLanguage_WhenNativeNameIsEmpty_ReturnsUnprocessableEntity()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/v1/languages",
            new
            {
                code = "de-DE",
                name = "German",
                nativeName = "",
                isDefault = false,
            });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
    }

    [Test]
    public async Task CreateLanguage_WhenCodeAlreadyExists_ReturnsConflict()
    {
        const string code = "it-IT";

        HttpResponseMessage firstResponse = await _client.PostAsJsonAsync(
            "/api/v1/languages",
            new
            {
                code,
                name = "Italian",
                nativeName = "Italiano",
                isDefault = false,
            });

        HttpResponseMessage duplicateResponse = await _client.PostAsJsonAsync(
            "/api/v1/languages",
            new
            {
                code,
                name = "Italian",
                nativeName = "Italiano",
                isDefault = false,
            });

        Assert.That(firstResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(duplicateResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task SetActive_WhenDeactivatingDefaultLanguage_ReturnsConflict()
    {
        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/api/v1/languages",
            new
            {
                code = "en-US",
                name = "English",
                nativeName = "English",
                isDefault = true,
            });

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            "/api/v1/languages/en-US/active",
            new { isActive = false });

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
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

    private static async Task<string> GetReadyHealthDetailsAsync()
    {
        using IServiceScope scope = ApiIntegrationTestHost.Factory.Services.CreateScope();
        var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();
        HealthReport report = await healthCheckService.CheckHealthAsync(
            registration => registration.Tags.Contains("ready"));

        return string.Join(
            "; ",
            report.Entries.Select(entry =>
                $"{entry.Key}={entry.Value}" +
                (string.IsNullOrWhiteSpace(entry.Value.Description)
                    ? string.Empty
                    : $" ({entry.Value.Description})") +
                (entry.Value.Exception is null
                    ? string.Empty
                    : $" [{entry.Value.Exception.GetType().Name}: {entry.Value.Exception.Message}]")));
    }
}
