using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using KUKULCAN.SharedKernel.i18n.Client.Models;

namespace KUKULCAN.SharedKernel.i18n.Client.ApiClient;

/// <summary>Typed HTTP client for the KUKULCAN.SharedKernel.i18n REST API.</summary>
public sealed class I18NApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private async Task<ApiResult<T>> GetAsync<T>(string url, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await http.GetAsync(url, ct);
        return await ParseAsync<T>(response);
    }

    private async Task<ApiResult<T>> PostAsync<T>(string url, object body, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await http.PostAsJsonAsync(url, body, JsonOptions, ct);
        return await ParseAsync<T>(response);
    }

    private async Task<ApiResult<T>> PutAsync<T>(string url, object body, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await http.PutAsJsonAsync(url, body, JsonOptions, ct);
        return await ParseAsync<T>(response);
    }

    private async Task<ApiResult<Unit>> PatchAsync(string url, object body, CancellationToken ct = default)
    {
        using var content = JsonContent.Create(body, options: JsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
        using HttpResponseMessage response = await http.SendAsync(request, ct);
        return response.IsSuccessStatusCode
            ? ApiResult<Unit>.Ok(Unit.Value, response.StatusCode)
            : ApiResult<Unit>.Fail(await ReadError(response), response.StatusCode);
    }

    private async Task<ApiResult<Unit>> DeleteAsync(string url, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await http.DeleteAsync(url, ct);
        return response.IsSuccessStatusCode
            ? ApiResult<Unit>.Ok(Unit.Value, response.StatusCode)
            : ApiResult<Unit>.Fail(await ReadError(response), response.StatusCode);
    }

    private static async Task<ApiResult<T>> ParseAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            T? result = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
            return ApiResult<T>.Ok(result!, response.StatusCode);
        }

        return ApiResult<T>.Fail(await ReadError(response), response.StatusCode);
    }

    private static async Task<ApiError> ReadError(HttpResponseMessage response)
    {
        try
        {
            ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>(JsonOptions);
            return error ?? new ApiError(response.ReasonPhrase, (int)response.StatusCode, null);
        }
        catch
        {
            return new ApiError(response.ReasonPhrase, (int)response.StatusCode, null);
        }
    }

    // Languages
    public Task<ApiResult<IReadOnlyList<LanguageDto>>> GetAllLanguagesAsync(bool activeOnly = true, CancellationToken ct = default)
        => GetAsync<IReadOnlyList<LanguageDto>>($"api/v1/languages?activeOnly={activeOnly}", ct);

    public Task<ApiResult<LanguageDto>> GetLanguageAsync(string code, CancellationToken ct = default)
        => GetAsync<LanguageDto>($"api/v1/languages/{Uri.EscapeDataString(code)}", ct);

    public Task<ApiResult<LanguageDto>> CreateLanguageAsync(CreateLanguageRequest body, CancellationToken ct = default)
        => PostAsync<LanguageDto>("api/v1/languages", body, ct);

    public Task<ApiResult<LanguageDto>> UpdateLanguageAsync(string code, UpdateLanguageRequest body, CancellationToken ct = default)
        => PutAsync<LanguageDto>($"api/v1/languages/{Uri.EscapeDataString(code)}", body, ct);

    public Task<ApiResult<Unit>> SetLanguageActiveAsync(string code, bool isActive, CancellationToken ct = default)
        => PatchAsync($"api/v1/languages/{Uri.EscapeDataString(code)}/active", new SetActiveRequest(isActive), ct);

    public Task<ApiResult<Unit>> SetDefaultLanguageAsync(string code, CancellationToken ct = default)
        => PatchAsync($"api/v1/languages/{Uri.EscapeDataString(code)}/default", new { }, ct);

    // Locales
    public Task<ApiResult<IReadOnlyList<LocaleConfigurationDto>>> GetAllLocalesAsync(CancellationToken ct = default)
        => GetAsync<IReadOnlyList<LocaleConfigurationDto>>("api/v1/locales", ct);

    public Task<ApiResult<LocaleConfigurationDto>> GetLocaleAsync(string languageCode, CancellationToken ct = default)
        => GetAsync<LocaleConfigurationDto>($"api/v1/locales/{Uri.EscapeDataString(languageCode)}", ct);

    public Task<ApiResult<LocaleConfigurationDto>> UpsertLocaleAsync(string languageCode, UpsertLocaleRequest body, CancellationToken ct = default)
        => PutAsync<LocaleConfigurationDto>($"api/v1/locales/{Uri.EscapeDataString(languageCode)}", body, ct);

    // Currencies
    public Task<ApiResult<IReadOnlyList<CurrencyFormatDto>>> GetCurrenciesAsync(string languageCode, CancellationToken ct = default)
        => GetAsync<IReadOnlyList<CurrencyFormatDto>>($"api/v1/currencies/{Uri.EscapeDataString(languageCode)}", ct);

    public Task<ApiResult<CurrencyFormatDto>> UpsertCurrencyAsync(string languageCode, string currencyCode, UpsertCurrencyRequest body, CancellationToken ct = default)
        => PutAsync<CurrencyFormatDto>($"api/v1/currencies/{Uri.EscapeDataString(languageCode)}/{Uri.EscapeDataString(currencyCode)}", body, ct);

    public Task<ApiResult<Unit>> DeleteCurrencyAsync(string languageCode, string currencyCode, CancellationToken ct = default)
        => DeleteAsync($"api/v1/currencies/{Uri.EscapeDataString(languageCode)}/{Uri.EscapeDataString(currencyCode)}", ct);

    // Translations
    public Task<ApiResult<TranslationLookupDto>> GetTranslationAsync(string code, string languageCode, CancellationToken ct = default)
        => GetAsync<TranslationLookupDto>($"api/v1/translations/{Uri.EscapeDataString(code)}/{Uri.EscapeDataString(languageCode)}", ct);

    public Task<ApiResult<IReadOnlyList<TranslationDto>>> GetTranslationVariantsAsync(string code, CancellationToken ct = default)
        => GetAsync<IReadOnlyList<TranslationDto>>($"api/v1/translations/{Uri.EscapeDataString(code)}/variants", ct);

    public Task<ApiResult<TranslationMapDto>> GetModuleTranslationsAsync(string module, string languageCode, CancellationToken ct = default)
        => GetAsync<TranslationMapDto>($"api/v1/translations/module/{Uri.EscapeDataString(module)}/{Uri.EscapeDataString(languageCode)}", ct);

    public Task<ApiResult<PagedResult<TranslationDto>>> GetTranslationsPagedAsync(
        int page = 1, int pageSize = 50, string? module = null, string? languageCode = null, string? sortBy = null,
        CancellationToken ct = default)
    {
        var query = $"api/v1/translations?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(module)) query += $"&module={Uri.EscapeDataString(module)}";
        if (!string.IsNullOrWhiteSpace(languageCode)) query += $"&languageCode={Uri.EscapeDataString(languageCode)}";
        if (!string.IsNullOrWhiteSpace(sortBy)) query += $"&sortBy={Uri.EscapeDataString(sortBy)}";
        return GetAsync<PagedResult<TranslationDto>>(query, ct);
    }

    public Task<ApiResult<TranslationDto>> CreateTranslationAsync(CreateTranslationRequest body, CancellationToken ct = default)
        => PostAsync<TranslationDto>("api/v1/translations", body, ct);

    public Task<ApiResult<TranslationDto>> UpdateTranslationAsync(string code, string languageCode, UpdateTranslationRequest body, CancellationToken ct = default)
        => PutAsync<TranslationDto>($"api/v1/translations/{Uri.EscapeDataString(code)}/{Uri.EscapeDataString(languageCode)}", body, ct);

    public Task<ApiResult<Unit>> SetTranslationReviewedAsync(string code, string languageCode, bool isReviewed, CancellationToken ct = default)
        => PatchAsync($"api/v1/translations/{Uri.EscapeDataString(code)}/{Uri.EscapeDataString(languageCode)}/review", new SetReviewedRequest(isReviewed), ct);

    public Task<ApiResult<Unit>> DeleteTranslationAsync(string code, string languageCode, CancellationToken ct = default)
        => DeleteAsync($"api/v1/translations/{Uri.EscapeDataString(code)}/{Uri.EscapeDataString(languageCode)}", ct);

    public Task<ApiResult<BulkUpsertResultDto>> BulkUpsertTranslationsAsync(BulkUpsertRequest body, CancellationToken ct = default)
        => PostAsync<BulkUpsertResultDto>("api/v1/translations/bulk", body, ct);
}

public sealed class ApiResult<T>
{
    public bool IsSuccess { get; private init; }
    public T? Value { get; private init; }
    public ApiError? Error { get; private init; }
    public HttpStatusCode StatusCode { get; private init; }

    public static ApiResult<T> Ok(T value, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new() { IsSuccess = true, Value = value, StatusCode = statusCode };

    public static ApiResult<T> Fail(ApiError error, HttpStatusCode statusCode)
        => new() { IsSuccess = false, Error = error, StatusCode = statusCode };
}

public readonly struct Unit
{
    public static readonly Unit Value = default;
}
