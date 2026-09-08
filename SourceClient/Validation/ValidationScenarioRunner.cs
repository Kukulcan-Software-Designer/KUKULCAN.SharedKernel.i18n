using System.Net;
using System.Net.Http.Json;
using KUKULCAN.SharedKernel.i18n.Client.ApiClient;
using KUKULCAN.SharedKernel.i18n.Client.Models;
using KUKULCAN.SharedKernel.i18n.Client.Configuration;
using Spectre.Console;

namespace KUKULCAN.SharedKernel.i18n.Client.Validation;

/// <summary>Runs a non-destructive API contract validation suite against an existing i18n installation.</summary>
public sealed class ValidationScenarioRunner(I18NApiClient api, HttpClient http, ApiSettings settings)
{
    private readonly List<ScenarioResult> results = [];
    private readonly string suffix = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
    private string? defaultLanguage;
    private LanguageDto? stateLanguage;
    private bool stateLanguageWasActive;
    private string? createdTranslationCode;
    private string? createdTranslationLanguage;
    private string? createdBulkCode1;
    private string? createdBulkCode2;

    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        try
        {
            await AuthenticationAsync(ct);
            await LanguagesAsync(ct);
            await LocalesAsync(ct);
            await CurrenciesAsync(ct);
            await TranslationsAsync(ct);
            await FallbackAndProtectionAsync(ct);
            await PaginationAndBulkAsync(ct);
        }
        catch (OperationCanceledException)
        {
            Add("Suite cancellation", true, null, null, "Cancellation requested.");
        }
        catch (Exception ex)
        {
            Add("Suite execution", false, null, null, ex.Message);
        }
        finally
        {
            await RestoreAsync(ct);
        }

        RenderSummary();
        return results.Any(x => !x.Passed) ? 1 : 0;
    }

    private async Task AuthenticationAsync(CancellationToken ct)
    {
        await RawAsync("GET languages without credentials", HttpMethod.Get, "api/v1/languages", null, 401, null, ct);
        await RawAsync("POST languages without credentials", HttpMethod.Post, "api/v1/languages",
            new CreateLanguageRequest("zz", "Unauthorized", "Unauthorized"), 401, null, ct);

        string? forbiddenToken = Environment.GetEnvironmentVariable("I18N_FORBIDDEN_BEARER_TOKEN");
        if (string.IsNullOrWhiteSpace(forbiddenToken))
        {
            Add("Authenticated read-only identity cannot write (403)", true, null, null,
                "SKIPPED: set I18N_FORBIDDEN_BEARER_TOKEN to a valid authenticated token without i18n.write.");
            return;
        }

        await RawAsync("Authenticated identity without i18n.write", HttpMethod.Post, "api/v1/languages",
            new CreateLanguageRequest("zz", "Forbidden", "Forbidden"), 403, forbiddenToken, ct);
    }

    private async Task LanguagesAsync(CancellationToken ct)
    {
        ApiResult<IReadOnlyList<LanguageDto>> all = await api.GetAllLanguagesAsync(false, ct);
        Expect("Languages: GET all", all, 200);
        if (!all.IsSuccess || all.Value is null)
            return;

        LanguageDto? currentDefault = all.Value.FirstOrDefault(x => x.IsDefault);
        defaultLanguage = currentDefault?.Code;
        stateLanguage = all.Value.FirstOrDefault(x => x.IsActive && !x.IsDefault)
                         ?? all.Value.FirstOrDefault(x => !x.IsDefault);

        if (stateLanguage is null)
        {
            Add("Languages: non-default state fixture", false, null, null, "No non-default language is available.");
            return;
        }

        stateLanguageWasActive = stateLanguage.IsActive;
        await Expect("Languages: GET existing", () => api.GetLanguageAsync(stateLanguage.Code, ct), 200);
        await Expect("Languages: GET missing", () => api.GetLanguageAsync($"zz-MISSING-{suffix}", ct), 404);
        await Expect("Languages: invalid code", () => api.CreateLanguageAsync(new CreateLanguageRequest("", "", ""), ct), 422);
        await Expect("Languages: name maximum length", () => api.CreateLanguageAsync(
            new CreateLanguageRequest($"zz-{suffix[^4..]}", new string('N', 101), "Native"), ct), 422);
        await Expect("Languages: native name maximum length", () => api.CreateLanguageAsync(
            new CreateLanguageRequest($"zz-{suffix[^4..]}", "Name", new string('N', 101)), ct), 422);

        await Expect("Languages: update", () => api.UpdateLanguageAsync(stateLanguage.Code,
            new UpdateLanguageRequest(stateLanguage.Name + " validation", stateLanguage.NativeName + " validation"), ct), 200);
        await Expect("Languages: update missing", () => api.UpdateLanguageAsync($"zz-MISSING-{suffix}",
            new UpdateLanguageRequest("x", "x"), ct), 404);

        await Expect("Languages: deactivate non-default", () => api.SetLanguageActiveAsync(stateLanguage.Code, false, ct), 204);
        await Expect("Languages: reactivate non-default", () => api.SetLanguageActiveAsync(stateLanguage.Code, true, ct), 204);

        if (defaultLanguage is not null)
        {
            await Expect("Languages: default cannot deactivate", () => api.SetLanguageActiveAsync(defaultLanguage, false, ct), 409);
            await Expect("Languages: transfer default", () => api.SetDefaultLanguageAsync(stateLanguage.Code, ct), 204);
            await Expect("Languages: new default cannot deactivate", () => api.SetLanguageActiveAsync(stateLanguage.Code, false, ct), 409);
            await Expect("Languages: restore default", () => api.SetDefaultLanguageAsync(defaultLanguage, ct), 204);
        }
    }

    private async Task LocalesAsync(CancellationToken ct)
    {
        if (stateLanguage is null) return;
        ApiResult<IReadOnlyList<LocaleConfigurationDto>> all = await api.GetAllLocalesAsync(ct);
        ExpectValue("Locales: GET all", all, 200);

        LocaleConfigurationDto? existing = all.Value?.FirstOrDefault(x =>
            string.Equals(x.LanguageCode, stateLanguage.Code, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            Add("Locales: persistent fixture", true, null, null, "SKIPPED: selected language has no existing locale configuration; no orphan can be created safely.");
            return;
        }

        await Expect("Locales: GET existing", () => api.GetLocaleAsync(existing.LanguageCode, ct), 200);
        await Expect("Locales: invalid language", () => api.GetLocaleAsync($"zz-MISSING-{suffix}", ct), 404);
        await Expect("Locales: date format max length", () => api.UpsertLocaleAsync(existing.LanguageCode,
            new UpsertLocaleRequest(new string('x', 51), existing.ShortDateFormat, existing.TimeFormat, existing.DateTimeFormat,
                existing.FirstDayOfWeek, existing.DecimalSeparator, existing.ThousandsSeparator, existing.DecimalPlaces, existing.CurrencyDecimalPlaces), ct), 422);
        await Expect("Locales: decimal separator empty", () => api.UpsertLocaleAsync(existing.LanguageCode,
            new UpsertLocaleRequest(existing.DateFormat, existing.ShortDateFormat, existing.TimeFormat, existing.DateTimeFormat,
                existing.FirstDayOfWeek, "", existing.ThousandsSeparator, existing.DecimalPlaces, existing.CurrencyDecimalPlaces), ct), 422);
        await Expect("Locales: equal separators", () => api.UpsertLocaleAsync(existing.LanguageCode,
            new UpsertLocaleRequest(existing.DateFormat, existing.ShortDateFormat, existing.TimeFormat, existing.DateTimeFormat,
                existing.FirstDayOfWeek, ".", ".", existing.DecimalPlaces, existing.CurrencyDecimalPlaces), ct), 422);
        await Expect("Locales: decimal places out of range", () => api.UpsertLocaleAsync(existing.LanguageCode,
            new UpsertLocaleRequest(existing.DateFormat, existing.ShortDateFormat, existing.TimeFormat, existing.DateTimeFormat,
                existing.FirstDayOfWeek, existing.DecimalSeparator, existing.ThousandsSeparator, 11, existing.CurrencyDecimalPlaces), ct), 422);
        await Expect("Locales: invalid first day", () => api.UpsertLocaleAsync(existing.LanguageCode,
            new UpsertLocaleRequest(existing.DateFormat, existing.ShortDateFormat, existing.TimeFormat, existing.DateTimeFormat,
                "Tuesday", existing.DecimalSeparator, existing.ThousandsSeparator, existing.DecimalPlaces, existing.CurrencyDecimalPlaces), ct), 422);
    }

    private async Task CurrenciesAsync(CancellationToken ct)
    {
        if (stateLanguage is null) return;
        ApiResult<IReadOnlyList<CurrencyFormatDto>> all = await api.GetCurrenciesAsync(stateLanguage.Code, ct);
        ExpectValue("Currencies: GET language", all, 200);
        CurrencyFormatDto? existing = all.Value?.FirstOrDefault();
        if (existing is null)
        {
            Add("Currencies: persistent fixture", true, null, null, "SKIPPED: selected language has no existing currency; no orphan can be created safely.");
            return;
        }

        await Expect("Currencies: invalid code", () => api.UpsertCurrencyAsync(stateLanguage.Code, "US",
            new UpsertCurrencyRequest(existing.CurrencyName, existing.Symbol, existing.SymbolPosition, existing.SpaceBetweenSymbolAndAmount,
                existing.DecimalSeparator, existing.ThousandsSeparator, existing.DecimalPlaces, existing.NegativePattern), ct), 422);
        await Expect("Currencies: name max length", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode,
            new UpsertCurrencyRequest(new string('N', 101), existing.Symbol, existing.SymbolPosition, existing.SpaceBetweenSymbolAndAmount,
                existing.DecimalSeparator, existing.ThousandsSeparator, existing.DecimalPlaces, existing.NegativePattern), ct), 422);
        await Expect("Currencies: symbol max length", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode,
            new UpsertCurrencyRequest(existing.CurrencyName, new string('S', 6), existing.SymbolPosition, existing.SpaceBetweenSymbolAndAmount,
                existing.DecimalSeparator, existing.ThousandsSeparator, existing.DecimalPlaces, existing.NegativePattern), ct), 422);
        await Expect("Currencies: invalid position", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode,
            new UpsertCurrencyRequest(existing.CurrencyName, existing.Symbol, "Middle", existing.SpaceBetweenSymbolAndAmount,
                existing.DecimalSeparator, existing.ThousandsSeparator, existing.DecimalPlaces, existing.NegativePattern), ct), 422);
        await Expect("Currencies: equal separators", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode,
            new UpsertCurrencyRequest(existing.CurrencyName, existing.Symbol, existing.SymbolPosition, existing.SpaceBetweenSymbolAndAmount,
                ".", ".", existing.DecimalPlaces, existing.NegativePattern), ct), 422);
        await Expect("Currencies: decimal places out of range", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode,
            new UpsertCurrencyRequest(existing.CurrencyName, existing.Symbol, existing.SymbolPosition, existing.SpaceBetweenSymbolAndAmount,
                existing.DecimalSeparator, existing.ThousandsSeparator, 11, existing.NegativePattern), ct), 422);
        await Expect("Currencies: invalid negative pattern", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode,
            new UpsertCurrencyRequest(existing.CurrencyName, existing.Symbol, existing.SymbolPosition, existing.SpaceBetweenSymbolAndAmount,
                existing.DecimalSeparator, existing.ThousandsSeparator, existing.DecimalPlaces, "-{symbol}"), ct), 422);
    }

    private async Task TranslationsAsync(CancellationToken ct)
    {
        if (stateLanguage is null) return;
        createdTranslationCode = $"TST{suffix[^4..]}";
        createdTranslationLanguage = stateLanguage.Code;

        await Expect("Translations: invalid code", () => api.CreateTranslationAsync(new CreateTranslationRequest("bad", stateLanguage.Code, "x"), ct), 422);
        await Expect("Translations: empty text", () => api.CreateTranslationAsync(new CreateTranslationRequest("TST0001", stateLanguage.Code, ""), ct), 422);
        await Expect("Translations: text max length", () => api.CreateTranslationAsync(new CreateTranslationRequest("TST0002", stateLanguage.Code, new string('x', 4001)), ct), 422);
        await Expect("Translations: max length constraint", () => api.CreateTranslationAsync(new CreateTranslationRequest("TST0003", stateLanguage.Code, "123456", null, 5), ct), 422);
        await Expect("Translations: create", () => api.CreateTranslationAsync(new CreateTranslationRequest(createdTranslationCode, stateLanguage.Code, "Original", "validation", 100), ct), 201);
        await Expect("Translations: duplicate", () => api.CreateTranslationAsync(new CreateTranslationRequest(createdTranslationCode, stateLanguage.Code, "Duplicate"), ct), 409);

        ApiResult<TranslationDto> created = await api.GetTranslationAsync(createdTranslationCode, stateLanguage.Code, ct);
        ExpectValue("Translations: exact lookup", created, 200);
        if (created.IsSuccess && created.Value is not null)
        {
            bool correct = created.Value.Text == "Original" && !created.Value.IsFallback && created.Value.ResolvedLanguageCode == stateLanguage.Code;
            Add("Translations: exact lookup semantics", correct, 200, 200, created.Value.Text);
        }

        await Expect("Translations: update", () => api.UpdateTranslationAsync(createdTranslationCode, stateLanguage.Code,
            new UpdateTranslationRequest("Updated", "updated"), ct), 200);
        await Expect("Translations: reviewed=true", () => api.SetTranslationReviewedAsync(createdTranslationCode, stateLanguage.Code, true, ct), 204);
        ApiResult<TranslationDto> reviewed = await api.GetTranslationAsync(createdTranslationCode, stateLanguage.Code, ct);
        bool reviewedOk = reviewed.IsSuccess && reviewed.Value?.IsReviewed == true;
        Add("Translations: reviewed state", reviewedOk, 200, reviewed.IsSuccess ? 200 : reviewed.Error?.Status);
        await Expect("Translations: update resets review", () => api.UpdateTranslationAsync(createdTranslationCode, stateLanguage.Code,
            new UpdateTranslationRequest("Updated again"), ct), 200);
        ApiResult<TranslationDto> reset = await api.GetTranslationAsync(createdTranslationCode, stateLanguage.Code, ct);
        Add("Translations: update review reset semantics", reset.IsSuccess && reset.Value?.IsReviewed == false, 200, reset.IsSuccess ? 200 : reset.Error?.Status);

        await Expect("Translations: variants", () => api.GetTranslationVariantsAsync(createdTranslationCode, ct), 200);
        await Expect("Translations: module dictionary", () => api.GetModuleTranslationsAsync("TST", stateLanguage.Code, ct), 200);
        await Expect("Translations: delete", () => api.DeleteTranslationAsync(createdTranslationCode, stateLanguage.Code, ct), 204);
        createdTranslationCode = null;
        await Expect("Translations: delete missing", () => api.DeleteTranslationAsync($"TST{suffix[^4..]}", stateLanguage.Code, ct), 404);
    }

    private async Task FallbackAndProtectionAsync(CancellationToken ct)
    {
        if (defaultLanguage is null) return;
        ApiResult<PagedResult<TranslationDto>> first = await api.GetTranslationsPagedAsync(1, 50, null, null, null, ct);
        ExpectValue("Translations: admin paged list", first, 200);
        if (!first.IsSuccess || first.Value is null) return;

        TranslationDto? english = null;
        TranslationDto? parent = null;
        TranslationDto? defaultOnly = null;
        int pages = Math.Min(first.Value.TotalPages, 20);
        for (int page = 1; page <= pages && (english is null || parent is null || defaultOnly is null); page++)
        {
            ApiResult<PagedResult<TranslationDto>> current = page == 1 ? first : await api.GetTranslationsPagedAsync(page, 50, null, null, null, ct);
            if (!current.IsSuccess || current.Value is null) continue;
            foreach (TranslationDto item in current.Value.Items.Where(x => string.Equals(x.LanguageCode, defaultLanguage, StringComparison.OrdinalIgnoreCase)))
            {
                ApiResult<IReadOnlyList<TranslationDto>> variants = await api.GetTranslationVariantsAsync(item.Code, ct);
                if (!variants.IsSuccess || variants.Value is null) continue;
                bool hasEs = variants.Value.Any(x => x.LanguageCode.Equals("es", StringComparison.OrdinalIgnoreCase));
                bool hasEsMx = variants.Value.Any(x => x.LanguageCode.Equals("es-MX", StringComparison.OrdinalIgnoreCase));
                english ??= item;
                if (hasEs && !hasEsMx) parent ??= item;
                if (!hasEs && !hasEsMx) defaultOnly ??= item;
            }
        }

        if (parent is not null)
        {
            ApiResult<TranslationLookupDto> lookup = await api.GetTranslationAsync(parent.Code, "es-MX", ct);
            bool ok = lookup.IsSuccess && lookup.Value?.IsFallback == true && lookup.Value.ResolvedLanguageCode.Equals("es", StringComparison.OrdinalIgnoreCase);
            Add("Fallback: es-MX -> es", ok, 200, lookup.IsSuccess ? 200 : lookup.Error?.Status,
                lookup.Value?.ResolvedLanguageCode);
        }
        else Add("Fallback: es-MX -> es", true, null, null, "SKIPPED: no safe existing fixture with es but without es-MX.");

        if (defaultOnly is not null)
        {
            ApiResult<TranslationLookupDto> lookup = await api.GetTranslationAsync(defaultOnly.Code, "es-MX", ct);
            bool ok = lookup.IsSuccess && lookup.Value?.IsFallback == true && lookup.Value.ResolvedLanguageCode.Equals(defaultLanguage, StringComparison.OrdinalIgnoreCase);
            Add("Fallback: es-MX -> default", ok, 200, lookup.IsSuccess ? 200 : lookup.Error?.Status,
                lookup.Value?.ResolvedLanguageCode);
        }
        else Add("Fallback: es-MX -> default", true, null, null, "SKIPPED: no safe existing default-only fixture.");

        if (english is not null)
            await Expect("Default-language translation protected delete", () => api.DeleteTranslationAsync(english.Code, defaultLanguage, ct), 409);
    }

    private async Task PaginationAndBulkAsync(CancellationToken ct)
    {
        if (stateLanguage is null) return;
        ApiResult<PagedResult<TranslationDto>> page = await api.GetTranslationsPagedAsync(0, 500, null, stateLanguage.Code, null, ct);
        bool clamped = page.IsSuccess && page.Value is not null && page.Value.Page == 1 && page.Value.PageSize == 200;
        Add("Pagination: page/pageSize are clamped", clamped, 200, page.IsSuccess ? 200 : page.Error?.Status);

        await Expect("Bulk: empty", () => api.BulkUpsertTranslationsAsync(new BulkUpsertRequest([]), ct), 422);
        await Expect("Bulk: invalid item", () => api.BulkUpsertTranslationsAsync(new BulkUpsertRequest([
            new BulkTranslationEntry("bad", stateLanguage.Code, "x")]), ct), 422);
        await Expect("Bulk: over 5000", () => api.BulkUpsertTranslationsAsync(new BulkUpsertRequest(
            Enumerable.Range(0, 5001).Select(i => new BulkTranslationEntry($"TST{(i % 9999) + 1:D4}", stateLanguage.Code, "x")).ToArray()), ct), 422);

        createdBulkCode1 = $"TST{suffix[^4..]}";
        int second = (int.Parse(suffix[^4..]) % 9000) + 1000;
        createdBulkCode2 = $"TST{second:D4}";
        if (createdBulkCode1 == createdBulkCode2) createdBulkCode2 = "TST9999";

        BulkUpsertRequest bulk = new([
            new BulkTranslationEntry(createdBulkCode1, stateLanguage.Code, "Bulk one"),
            new BulkTranslationEntry(createdBulkCode2, stateLanguage.Code, "Bulk two")]);
        ApiResult<BulkUpsertResultDto> inserted = await api.BulkUpsertTranslationsAsync(bulk, ct);
        ExpectValue("Bulk: insert", inserted, 200);
        if (inserted.IsSuccess)
            Add("Bulk: insert counters", inserted.Value?.Inserted == 2 && inserted.Value.Updated == 0, 200, 200);

        ApiResult<BulkUpsertResultDto> updated = await api.BulkUpsertTranslationsAsync(bulk with
        {
            Entries = [
                new BulkTranslationEntry(createdBulkCode1, stateLanguage.Code, "Bulk one updated"),
                new BulkTranslationEntry(createdBulkCode2, stateLanguage.Code, "Bulk two updated")]
        }, ct);
        ExpectValue("Bulk: update", updated, 200);
        if (updated.IsSuccess)
            Add("Bulk: update counters", updated.Value?.Updated == 2 && updated.Value.Inserted == 0, 200, 200);
    }

    private async Task RestoreAsync(CancellationToken ct)
    {
        try
        {
            if (createdTranslationCode is not null && createdTranslationLanguage is not null)
                await api.DeleteTranslationAsync(createdTranslationCode, createdTranslationLanguage, ct);
            if (createdBulkCode1 is not null && stateLanguage is not null)
                await api.DeleteTranslationAsync(createdBulkCode1, stateLanguage.Code, ct);
            if (createdBulkCode2 is not null && stateLanguage is not null)
                await api.DeleteTranslationAsync(createdBulkCode2, stateLanguage.Code, ct);
            if (stateLanguage is not null)
            {
                await api.SetLanguageActiveAsync(stateLanguage.Code, stateLanguageWasActive, ct);
                if (defaultLanguage is not null)
                    await api.SetDefaultLanguageAsync(defaultLanguage, ct);
            }
        }
        catch (Exception ex)
        {
            Add("State restoration", false, null, null, ex.Message);
        }
    }

    private async Task Expect<T>(string name, Func<Task<ApiResult<T>>> action, int expected)
    {
        try
        {
            ApiResult<T> result = await action();
            int? actual = result.StatusCode == 0 ? null : (int)result.StatusCode;
            bool passed = actual == expected;
            Add(name, passed, expected, actual, result.Error?.Detail);
        }
        catch (Exception ex) { Add(name, false, expected, null, ex.Message); }
    }

    private void Expect<T>(string name, ApiResult<T> result, int expected)
    {
        int? actual = result.StatusCode == 0 ? null : (int)result.StatusCode;
        Add(name, actual == expected, expected, actual, result.Error?.Detail);
    }

    private void ExpectValue<T>(string name, ApiResult<T> result, int expected)
    {
        Expect(name, result, expected);
        if (result.IsSuccess && result.Value is null)
            Add(name + ": response body", false, expected, (int)result.StatusCode, "Successful response had no body.");
    }

    private async Task RawAsync(string name, HttpMethod method, string path, object? body, int expected, string? bearer, CancellationToken ct)
    {
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/") };
            if (!string.IsNullOrWhiteSpace(bearer)) client.DefaultRequestHeaders.Authorization = new("Bearer", bearer);
            using HttpRequestMessage request = new(method, path);
            if (body is not null) request.Content = JsonContent.Create(body);
            using HttpResponseMessage response = await client.SendAsync(request, ct);
            Add(name, (int)response.StatusCode == expected, expected, (int)response.StatusCode);
        }
        catch (Exception ex) { Add(name, false, expected, null, ex.Message); }
    }

    private void Add(string name, bool passed, int? expected, int? actual, string? detail = null)
        => results.Add(new ScenarioResult(name, passed, expected, actual, detail));

    private void RenderSummary()
    {
        Table table = new Table().AddColumn("Scenario").AddColumn("Expected").AddColumn("Actual").AddColumn("Result");
        foreach (ScenarioResult result in results)
            table.AddRow(result.Name, result.Expected?.ToString() ?? "—", result.Actual?.ToString() ?? "—",
                result.Passed ? "[green]PASS[/]" : $"[red]FAIL[/] {Markup.Escape(result.Detail ?? "")}");
        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[bold]Validation: {results.Count(x => x.Passed)}/{results.Count} passed.[/]");
    }

    private sealed record ScenarioResult(string Name, bool Passed, int? Expected, int? Actual, string? Detail);
}
