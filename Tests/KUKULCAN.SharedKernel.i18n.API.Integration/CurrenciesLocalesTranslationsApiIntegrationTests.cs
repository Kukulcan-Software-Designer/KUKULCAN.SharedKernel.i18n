using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.i18n.API.Integration;

[TestFixture]
public sealed class CurrenciesLocalesTranslationsApiIntegrationTests
{
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUpAsync()
    {
        await ApiIntegrationTestHost.ResetDatabaseAsync();
        await EnsureLanguageAsync("es-ES");

        _client = ApiIntegrationTestHost.Factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthenticationHandler.SchemeName);
    }

    [TearDown]
    public void TearDown() => _client.Dispose();

    [Test]
    public async Task CurrencyEndpoints_UpsertGetAndDelete_PersistThroughRealApi()
    {
        HttpResponseMessage upsert = await _client.PutAsJsonAsync(
            "/api/v1/currencies/es-ES/EUR",
            new
            {
                currencyName = "Euro",
                symbol = "€",
                symbolPosition = "Before",
                spaceBetweenSymbolAndAmount = false,
                decimalSeparator = ",",
                thousandsSeparator = ".",
                decimalPlaces = 2,
                negativePattern = "-{symbol}{amount}",
            });

        Assert.That(upsert.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        HttpResponseMessage get = await _client.GetAsync("/api/v1/currencies/es-ES");
        Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        string body = await get.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("EUR"));
        Assert.That(body, Does.Contain("Euro"));

        HttpResponseMessage delete = await _client.DeleteAsync("/api/v1/currencies/es-ES/EUR");
        Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        HttpResponseMessage getAfterDelete = await _client.GetAsync("/api/v1/currencies/es-ES");
        Assert.That(getAfterDelete.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await getAfterDelete.Content.ReadAsStringAsync(), Does.Not.Contain("EUR"));
    }

    [Test]
    public async Task LocaleEndpoints_UpsertGetAndGetAll_ReturnExpectedData()
    {
        HttpResponseMessage upsert = await _client.PutAsJsonAsync(
            "/api/v1/locales/es-ES",
            new
            {
                dateFormat = "dd/MM/yyyy",
                shortDateFormat = "dd/MM/yy",
                timeFormat = "HH:mm:ss",
                dateTimeFormat = "dd/MM/yyyy HH:mm:ss",
                firstDayOfWeek = "Monday",
                decimalSeparator = ",",
                thousandsSeparator = ".",
                decimalPlaces = 2,
                currencyDecimalPlaces = 2,
            });

        Assert.That(upsert.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        HttpResponseMessage get = await _client.GetAsync("/api/v1/locales/es-ES");
        Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        string getBody = await get.Content.ReadAsStringAsync();
        Assert.That(getBody, Does.Contain("dd/MM/yyyy"));
        Assert.That(getBody, Does.Contain("Monday"));

        HttpResponseMessage getAll = await _client.GetAsync("/api/v1/locales");
        Assert.That(getAll.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await getAll.Content.ReadAsStringAsync(), Does.Contain("es-ES"));
    }

    [Test]
    public async Task TranslationEndpoints_CreateReadUpdateReviewVariantsModuleAndPaged_ReturnExpectedResults()
    {
        const string code = "CRM0001";
        const string languageCode = "es-ES";

        HttpResponseMessage create = await _client.PostAsJsonAsync(
            "/api/v1/translations",
            new
            {
                code,
                languageCode,
                text = "Hola mundo",
                context = "Integration test",
                maxLength = 100,
            });

        Assert.That(create.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        HttpResponseMessage get = await _client.GetAsync($"/api/v1/translations/{code}/{languageCode}");
        Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await get.Content.ReadAsStringAsync(), Does.Contain("Hola mundo"));

        HttpResponseMessage update = await _client.PutAsJsonAsync(
            $"/api/v1/translations/{code}/{languageCode}",
            new
            {
                text = "Hola mundo actualizado",
                context = "Updated integration test",
            });

        Assert.That(update.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await update.Content.ReadAsStringAsync(), Does.Contain("Hola mundo actualizado"));

        HttpResponseMessage reviewed = await _client.PatchAsJsonAsync(
            $"/api/v1/translations/{code}/{languageCode}/review",
            new { isReviewed = true });

        Assert.That(reviewed.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        HttpResponseMessage variants = await _client.GetAsync($"/api/v1/translations/{code}/variants");
        Assert.That(variants.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await variants.Content.ReadAsStringAsync(), Does.Contain(languageCode));

        HttpResponseMessage module = await _client.GetAsync($"/api/v1/translations/module/CRM/{languageCode}");
        Assert.That(module.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await module.Content.ReadAsStringAsync(), Does.Contain(code));
        Assert.That(await module.Content.ReadAsStringAsync(), Does.Contain("Hola mundo actualizado"));

        HttpResponseMessage paged = await _client.GetAsync(
            "/api/v1/translations?page=1&pageSize=50&module=CRM&languageCode=es-ES");
        Assert.That(paged.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await paged.Content.ReadAsStringAsync(), Does.Contain(code));

        HttpResponseMessage delete = await _client.DeleteAsync($"/api/v1/translations/{code}/{languageCode}");
        Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        HttpResponseMessage getAfterDelete = await _client.GetAsync($"/api/v1/translations/{code}/{languageCode}");
        Assert.That(getAfterDelete.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    private static async Task EnsureLanguageAsync(string code)
    {
        using IServiceScope scope = ApiIntegrationTestHost.Factory.Services.CreateScope();
        I18NDbContext context = scope.ServiceProvider.GetRequiredService<I18NDbContext>();

        context.Languages.Add(Language.Create(
            Guid.CreateVersion7(), code, "Spanish", "Español").Value);

        await context.SaveChangesAsync();
    }
}
