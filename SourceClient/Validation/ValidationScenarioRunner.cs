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
    private bool suiteCancelled;

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
            suiteCancelled = true;
            Skip("Suite cancellation", "Cancellation requested; validation suite did not complete.");
        }
        catch (Exception ex)
        {
            Add("Suite execution", false, null, null, ex.Message);
        }
        finally
        {
            await RestoreAsync();
        }

        RenderSummary();
        return suiteCancelled || results.Any(x => x.Status == ScenarioStatus.Fail) ? 1 : 0;
    }

    private async Task AuthenticationAsync(CancellationToken ct)
    {
        await RawAsync("GET languages without credentials", HttpMethod.Get, "api/v1/languages", null, 401, null, ct);
        await RawAsync("POST languages without credentials", HttpMethod.Post, "api/v1/languages",
            new CreateLanguageRequest("zz", "Unauthorized", "Unauthorized"), 401, null, ct);

        string? forbiddenToken = Environment.GetEnvironmentVariable("I18N_FORBIDDEN_BEARER_TOKEN");
        if (string.IsNullOrWhiteSpace(forbiddenToken))
        {
            Skip("Authenticated read-only identity cannot write (403)",
                "Set I18N_FORBIDDEN_BEARER_TOKEN to a valid authenticated token without i18n.write.");
            return;
        }

        await RawAsync("Authenticated identity without i18n.write", HttpMethod.Post, "api/v1/languages",
            new CreateLanguageRequest("zz", "Forbidden", "Forbidden"), 403, forbiddenToken, ct);
    }

    private async Task LanguagesAsync(CancellationToken ct)
    {
        ApiResult<IReadOnlyList<LanguageDto>> all = await api.GetAllLanguagesAsync(false, ct);
        Expect("Languages: GET all", all, 200);
        if (!all.IsSuccess || all.Value is null) return;

        defaultLanguage = all.Value.FirstOrDefault(x => x.IsDefault)?.Code;
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
        await Expect("Languages: duplicate", () => api.CreateLanguageAsync(
            new CreateLanguageRequest(stateLanguage.Code, stateLanguage.Name, stateLanguage.NativeName), ct), 409);

        await Expect("Languages: update", () => api.UpdateLanguageAsync(stateLanguage.Code,
            new UpdateLanguageRequest(stateLanguage.Name + " validation", stateLanguage.NativeName + " validation"), ct), 200);
        await Expect("Languages: update empty name", () => api.UpdateLanguageAsync(stateLanguage.Code,
            new UpdateLanguageRequest("", stateLanguage.NativeName), ct), 422);
        await Expect("Languages: update empty native name", () => api.UpdateLanguageAsync(stateLanguage.Code,
            new UpdateLanguageRequest(stateLanguage.Name, ""), ct), 422);
        await Expect("Languages: update missing", () => api.UpdateLanguageAsync($"zz-MISSING-{suffix}",
            new UpdateLanguageRequest("x", "x"), ct), 404);
        await Expect("Languages: deactivate non-default", () => api.SetLanguageActiveAsync(stateLanguage.Code, false, ct), 204);
        await Expect("Languages: reactivate non-default", () => api.SetLanguageActiveAsync(stateLanguage.Code, true, ct), 204);

        if (defaultLanguage is not null)
        {
            await Expect("Languages: default cannot deactivate", () => api.SetLanguageActiveAsync(defaultLanguage, false, ct), 409);
            await Expect("Languages: transfer default", () => api.SetDefaultLanguageAsync(stateLanguage.Code, ct), 204);
            await Expect("Languages: new default cannot deactivate", () => api.SetLanguageActiveAsync(stateLanguage.Code, false, ct), 409);
            await Expect("Languages: inactive language cannot become default", async () =>
            {
                ApiResult<Unit> reactivated = await api.SetLanguageActiveAsync(stateLanguage.Code, true, ct);
                if (!reactivated.IsSuccess) return reactivated;
                await api.SetLanguageActiveAsync(stateLanguage.Code, false, ct);
                return await api.SetDefaultLanguageAsync(stateLanguage.Code, ct);
            }, 409);
            await Expect("Languages: restore default", () => api.SetDefaultLanguageAsync(defaultLanguage, ct), 204);
            await Expect("Languages: restore state language active", () => api.SetLanguageActiveAsync(stateLanguage.Code, stateLanguageWasActive, ct), 204);
        }
    }

    private async Task LocalesAsync(CancellationToken ct)
    {
        if (stateLanguage is null) return;
        ApiResult<IReadOnlyList<LocaleConfigurationDto>> all = await api.GetAllLocalesAsync(ct);
        ExpectValue("Locales: GET all", all, 200);
        LocaleConfigurationDto? existing = all.Value?.FirstOrDefault(x => x.LanguageCode.Equals(stateLanguage.Code, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            Skip("Locales: persistent fixture", "Selected language has no existing locale; no orphan can be created safely.");
            return;
        }

        await Expect("Locales: GET existing", () => api.GetLocaleAsync(existing.LanguageCode, ct), 200);
        await Expect("Locales: GET missing", () => api.GetLocaleAsync($"zz-MISSING-{suffix}", ct), 404);
        await Expect("Locales: PUT missing language", () => api.UpsertLocaleAsync($"zz-MISSING-{suffix}", existing.ToRequest(), ct), 404);
        await Expect("Locales: date format empty", () => api.UpsertLocaleAsync(existing.LanguageCode, existing.ToRequest(dateFormat: ""), ct), 422);
        await Expect("Locales: date format maximum length", () => api.UpsertLocaleAsync(existing.LanguageCode, existing.ToRequest(dateFormat: new string('x', 51)), ct), 422);
        await Expect("Locales: short date format empty", () => api.UpsertLocaleAsync(existing.LanguageCode, existing.ToRequest(shortDateFormat: ""), ct), 422);
        await Expect("Locales: short date format maximum length", () => api.UpsertLocaleAsync(existing.LanguageCode, existing.ToRequest(shortDateFormat: new string('x', 51)), ct), 422);
        await Expect("Locales: time format empty", () => api.UpsertLocaleAsync(existing.LanguageCode, existing.ToRequest(timeFormat: ""), ct), 422);
        await Expect("Locales: time format maximum length", () => api.UpsertLocaleAsync(existing.LanguageCode, existing.ToRequest(timeFormat: new string('x', 51)), ct), 422);
        await Expect("Locales: date-time format empty", () => api.UpsertLocaleAsync(existing.LanguageCode, existing.ToRequest(dateTimeFormat: ""), ct), 422);
        await Expect("Locales: date-time format maximum length", () => api.UpsertLocaleAsync(existing.LanguageCode, existing.ToRequest(dateTimeFormat: new string('x', 101)), ct), 422);
        await Expect("Locales: first day empty", () => api.UpsertLocaleAsync(existing.LanguageCode, existing.ToRequest(firstDayOfWeek: ""), ct), 422);
        await Expect("Locales: invalid first day", () => api.UpsertLocaleAsync(existing.LanguageCode, existing.ToRequest(firstDayOfWeek: "Tuesday"), ct), 422);
        await Expect("Locales: decimal separator empty", () => api.UpsertLocaleAsync(existing.LanguageCode, existing.ToRequest(decimalSeparator: ""), ct), 422);
        await Expect("Locales: decimal separator length", () => api.UpsertLocaleAsync(existing.LanguageCode, existing.ToRequest(decimalSeparator: ".."), ct), 422);
        await Expect("Locales: thousands separator empty", () => api.UpsertLocaleAsync(existing.LanguageCode, existing.ToRequest(thousandsSeparator: ""), ct), 422);
        await Expect("Locales: thousands separator length", () => api.UpsertLocaleAsync(existing.LanguageCode, existing.ToRequest(thousandsSeparator: ".."), ct), 422);
        await Expect("Locales: equal separators", () => api.UpsertLocaleAsync(existing.LanguageCode, existing.ToRequest(decimalSeparator: ".", thousandsSeparator: "."), ct), 422);
        await Expect("Locales: decimal places out of range", () => api.UpsertLocaleAsync(existing.LanguageCode, existing.ToRequest(decimalPlaces: 11), ct), 422);
        await Expect("Locales: currency decimal places out of range", () => api.UpsertLocaleAsync(existing.LanguageCode, existing.ToRequest(currencyDecimalPlaces: 11), ct), 422);
    }

    private async Task CurrenciesAsync(CancellationToken ct)
    {
        if (stateLanguage is null) return;
        ApiResult<IReadOnlyList<CurrencyFormatDto>> all = await api.GetCurrenciesAsync(stateLanguage.Code, ct);
        ExpectValue("Currencies: GET language", all, 200);
        CurrencyFormatDto? existing = all.Value?.FirstOrDefault();
        if (existing is null)
        {
            Skip("Currencies: persistent fixture", "Selected language has no existing currency; no orphan can be created safely.");
            return;
        }

        await Expect("Currencies: missing language upsert", () => api.UpsertCurrencyAsync($"zz-MISSING-{suffix}", existing.CurrencyCode, existing.ToRequest(), ct), 404);
        await Expect("Currencies: invalid code length", () => api.UpsertCurrencyAsync(stateLanguage.Code, "US", existing.ToRequest(), ct), 422);
        await Expect("Currencies: invalid code characters", () => api.UpsertCurrencyAsync(stateLanguage.Code, "U$D", existing.ToRequest(), ct), 422);
        await Expect("Currencies: name empty", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode, existing.ToRequest(currencyName: ""), ct), 422);
        await Expect("Currencies: name maximum length", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode, existing.ToRequest(currencyName: new string('N', 101)), ct), 422);
        await Expect("Currencies: symbol empty", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode, existing.ToRequest(symbol: ""), ct), 422);
        await Expect("Currencies: symbol maximum length", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode, existing.ToRequest(symbol: new string('S', 6)), ct), 422);
        await Expect("Currencies: invalid position", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode, existing.ToRequest(symbolPosition: "Middle"), ct), 422);
        await Expect("Currencies: decimal separator empty", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode, existing.ToRequest(decimalSeparator: ""), ct), 422);
        await Expect("Currencies: decimal separator length", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode, existing.ToRequest(decimalSeparator: ".."), ct), 422);
        await Expect("Currencies: thousands separator empty", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode, existing.ToRequest(thousandsSeparator: ""), ct), 422);
        await Expect("Currencies: thousands separator length", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode, existing.ToRequest(thousandsSeparator: ".."), ct), 422);
        await Expect("Currencies: equal separators", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode, existing.ToRequest(decimalSeparator: ".", thousandsSeparator: "."), ct), 422);
        await Expect("Currencies: decimal places out of range", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode, existing.ToRequest(decimalPlaces: 11), ct), 422);
        await Expect("Currencies: negative pattern empty", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode, existing.ToRequest(negativePattern: ""), ct), 422);
        await Expect("Currencies: negative pattern missing amount", () => api.UpsertCurrencyAsync(stateLanguage.Code, existing.CurrencyCode, existing.ToRequest(negativePattern: "-{symbol}"), ct), 422);
        await Expect("Currencies: delete missing", () => api.DeleteCurrencyAsync(stateLanguage.Code, "ZZZ", ct), 404);
    }

    private async Task TranslationsAsync(CancellationToken ct)
    {
        if (stateLanguage is null) return;
        createdTranslationCode = await FindUnusedCodeAsync(17, ct);
        createdTranslationLanguage = stateLanguage.Code;

        await Expect("Translations: invalid code", () => api.CreateTranslationAsync(new CreateTranslationRequest("bad", stateLanguage.Code, "x"), ct), 422);
        await Expect("Translations: empty text", () => api.CreateTranslationAsync(new CreateTranslationRequest(await FindUnusedCodeAsync(18, ct), stateLanguage.Code, ""), ct), 422);
        await Expect("Translations: text maximum length", () => api.CreateTranslationAsync(new CreateTranslationRequest(await FindUnusedCodeAsync(19, ct), stateLanguage.Code, new string('x', 4001)), ct), 422);
        await Expect("Translations: max length constraint", () => api.CreateTranslationAsync(new CreateTranslationRequest(await FindUnusedCodeAsync(20, ct), stateLanguage.Code, "123456", null, 5), ct), 422);
        await Expect("Translations: create", () => api.CreateTranslationAsync(new CreateTranslationRequest(createdTranslationCode, stateLanguage.Code, "Original", "validation", 100), ct), 201);
        await Expect("Translations: duplicate", () => api.CreateTranslationAsync(new CreateTranslationRequest(createdTranslationCode, stateLanguage.Code, "Duplicate"), ct), 409);

        ApiResult<TranslationDto> exact = await api.GetTranslationAsync(createdTranslationCode, stateLanguage.Code, ct);
        ExpectValue("Translations: exact lookup", exact, 200);
        if (exact.IsSuccess && exact.Value is not null)
            Add("Translations: exact lookup semantics", exact.Value.Text == "Original" && !exact.Value.IsReviewed &&
                exact.Value.LanguageCode.Equals(stateLanguage.Code, StringComparison.OrdinalIgnoreCase), 200, 200);

        await Expect("Translations: update", () => api.UpdateTranslationAsync(createdTranslationCode, stateLanguage.Code,
            new UpdateTranslationRequest("Updated", "updated"), ct), 200);
        await Expect("Translations: reviewed=true", () => api.SetTranslationReviewedAsync(createdTranslationCode, stateLanguage.Code, true, ct), 204);
        ApiResult<TranslationDto> reviewed = await api.GetTranslationAsync(createdTranslationCode, stateLanguage.Code, ct);
        Add("Translations: reviewed state", reviewed.IsSuccess && reviewed.Value?.IsReviewed == true, 200, reviewed.IsSuccess ? 200 : reviewed.Error?.Status);
        await Expect("Translations: update resets review", () => api.UpdateTranslationAsync(createdTranslationCode, stateLanguage.Code,
            new UpdateTranslationRequest("Updated again"), ct), 200);
        ApiResult<TranslationDto> reset = await api.GetTranslationAsync(createdTranslationCode, stateLanguage.Code, ct);
        Add("Translations: update review reset semantics", reset.IsSuccess && reset.Value?.IsReviewed == false, 200, reset.IsSuccess ? 200 : reset.Error?.Status);

        await Expect("Translations: variants", () => api.GetTranslationVariantsAsync(createdTranslationCode, ct), 200);
        await Expect("Translations: module dictionary", () => api.GetModuleTranslationsAsync("TST", stateLanguage.Code, ct), 200);
        await Expect("Translations: delete", () => api.DeleteTranslationAsync(createdTranslationCode, stateLanguage.Code, ct), 204);
        createdTranslationCode = null;
        await Expect("Translations: delete missing", () => api.DeleteTranslationAsync(await FindUnusedCodeAsync(21, ct), stateLanguage.Code, ct), 404);
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
        for (int page = 1; page <= Math.Min(first.Value.TotalPages, 20) && (parent is null || defaultOnly is null); page++)
        {
            ApiResult<PagedResult<TranslationDto>> current = page == 1 ? first : await api.GetTranslationsPagedAsync(page, 50, null, null, null, ct);
            if (!current.IsSuccess || current.Value is null) continue;
            foreach (TranslationDto item in current.Value.Items.Where(x => x.LanguageCode.Equals(defaultLanguage, StringComparison.OrdinalIgnoreCase)))
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
            Add("Fallback: es-MX -> es", ok, 200, lookup.IsSuccess ? 200 : lookup.Error?.Status, lookup.Value?.ResolvedLanguageCode);
        }
        else Skip("Fallback: es-MX -> es", "No safe existing es-without-es-MX fixture.");

        if (defaultOnly is not null)
        {
            ApiResult<TranslationLookupDto> lookup = await api.GetTranslationAsync(defaultOnly.Code, "es-MX", ct);
            bool ok = lookup.IsSuccess && lookup.Value?.IsFallback == true && lookup.Value.ResolvedLanguageCode.Equals(defaultLanguage, StringComparison.OrdinalIgnoreCase);
            Add("Fallback: es-MX -> default", ok, 200, lookup.IsSuccess ? 200 : lookup.Error?.Status, lookup.Value?.ResolvedLanguageCode);
        }
        else Skip("Fallback: es-MX -> default", "No safe existing default-only fixture.");

        if (english is not null)
            await Expect("Default-language translation protected delete", () => api.DeleteTranslationAsync(english.Code, defaultLanguage, ct), 409);
    }

    private async Task PaginationAndBulkAsync(CancellationToken ct)
    {
        if (stateLanguage is null) return;
        ApiResult<PagedResult<TranslationDto>> page = await api.GetTranslationsPagedAsync(0, 500, null, stateLanguage.Code, null, ct);
        Add("Pagination: page/pageSize clamped", page.IsSuccess && page.Value?.Page == 1 && page.Value.PageSize == 200,
            200, page.IsSuccess ? 200 : page.Error?.Status);

        await Expect("Bulk: empty", () => api.BulkUpsertTranslationsAsync(new BulkUpsertRequest([]), ct), 422);
        await Expect("Bulk: invalid code", () => api.BulkUpsertTranslationsAsync(new BulkUpsertRequest([
            new BulkTranslationEntry("bad", stateLanguage.Code, "x")]), ct), 422);
        await Expect("Bulk: over 5000", () => api.BulkUpsertTranslationsAsync(new BulkUpsertRequest(
            Enumerable.Range(0, 5001).Select(i => NextCode(i + 100)).ToArray()
                .Select(code => new BulkTranslationEntry(code, stateLanguage.Code, "x")).ToArray()), ct), 422);

        createdBulkCode1 = await FindUnusedCodeAsync(31, ct);
        createdBulkCode2 = await FindUnusedCodeAsync(32, ct);
        BulkUpsertRequest bulk = new([
            new BulkTranslationEntry(createdBulkCode1, stateLanguage.Code, "Bulk one"),
            new BulkTranslationEntry(createdBulkCode2, stateLanguage.Code, "Bulk two")]);
        ApiResult<BulkUpsertResultDto> inserted = await api.BulkUpsertTranslationsAsync(bulk, ct);
        ExpectValue("Bulk: insert", inserted, 200);
        if (inserted.IsSuccess) Add("Bulk: insert counters", inserted.Value?.Inserted == 2 && inserted.Value.Updated == 0, 200, 200);

        ApiResult<BulkUpsertResultDto> updated = await api.BulkUpsertTranslationsAsync(bulk with
        {
            Entries = [
                new BulkTranslationEntry(createdBulkCode1, stateLanguage.Code, "Bulk one updated"),
                new BulkTranslationEntry(createdBulkCode2, stateLanguage.Code, "Bulk two updated")]
        }, ct);
        ExpectValue("Bulk: update", updated, 200);
        if (updated.IsSuccess) Add("Bulk: update counters", updated.Value?.Updated == 2 && updated.Value.Inserted == 0, 200, 200);
    }

    private async Task RestoreAsync()
    {
        try
        {
            using CancellationTokenSource cleanupCts = new(TimeSpan.FromSeconds(30));
            CancellationToken ct = cleanupCts.Token;
            if (createdTranslationCode is not null && createdTranslationLanguage is not null)
                await api.DeleteTranslationAsync(createdTranslationCode, createdTranslationLanguage, ct);
            if (createdBulkCode1 is not null && stateLanguage is not null)
                await api.DeleteTranslationAsync(createdBulkCode1, stateLanguage.Code, ct);
            if (createdBulkCode2 is not null && stateLanguage is not null)
                await api.DeleteTranslationAsync(createdBulkCode2, stateLanguage.Code, ct);
            if (stateLanguage is not null)
            {
                await api.UpdateLanguageAsync(stateLanguage.Code, new UpdateLanguageRequest(stateLanguage.Name, stateLanguage.NativeName), ct);
                await api.SetLanguageActiveAsync(stateLanguage.Code, stateLanguageWasActive, ct);
                if (defaultLanguage is not null)
                    await api.SetDefaultLanguageAsync(defaultLanguage, ct);
            }
        }
        catch (Exception ex) { Add("State restoration", false, null, null, ex.Message); }
    }

    private async Task<string> FindUnusedCodeAsync(int salt, CancellationToken ct)
    {
        for (int offset = 0; offset < 900; offset++)
        {
            string code = NextCode(salt + offset);
            ApiResult<IReadOnlyList<TranslationDto>> variants = await api.GetTranslationVariantsAsync(code, ct);
            if (variants.IsSuccess && (variants.Value is null || variants.Value.Count == 0)) return code;
        }
        throw new InvalidOperationException("Unable to find an unused translation code in the validation range.");
    }

    private async Task Expect<T>(string name, Func<Task<ApiResult<T>>> action, int expected)
    {
        try
        {
            ApiResult<T> result = await action();
            int? actual = result.StatusCode == 0 ? null : (int)result.StatusCode;
            Add(name, actual == expected, expected, actual, result.Error?.Detail);
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

    private string NextCode(int salt)
    {
        int value = (int.Parse(suffix[^4..]) + salt) % 9999;
        if (value < 1) value = 1;
        return $"TST{value:D4}";
    }

    private void Skip(string name, string detail)
        => results.Add(new ScenarioResult(name, ScenarioStatus.Skip, null, null, detail));

    private void Add(string name, bool passed, int? expected, int? actual, string? detail = null)
        => results.Add(new ScenarioResult(name, passed ? ScenarioStatus.Pass : ScenarioStatus.Fail, expected, actual, detail));

    private void RenderSummary()
    {
        Table table = new Table().AddColumn("Scenario").AddColumn("Expected").AddColumn("Actual").AddColumn("Result");
        foreach (ScenarioResult result in results)
        {
            string rendered = result.Status switch
            {
                ScenarioStatus.Pass => "[green]PASS[/]",
                ScenarioStatus.Skip => $"[yellow]SKIP[/] {Markup.Escape(result.Detail ?? "")}",
                _ => $"[red]FAIL[/] {Markup.Escape(result.Detail ?? "")}"
            };
            table.AddRow(result.Name, result.Expected?.ToString() ?? "—", result.Actual?.ToString() ?? "—", rendered);
        }
        AnsiConsole.Write(table);
        int passed = results.Count(x => x.Status == ScenarioStatus.Pass);
        int skipped = results.Count(x => x.Status == ScenarioStatus.Skip);
        int failed = results.Count(x => x.Status == ScenarioStatus.Fail);
        AnsiConsole.MarkupLine($"[bold]Validation: {passed} passed, {skipped} skipped, {failed} failed.[/]");
    }

    private enum ScenarioStatus { Pass, Skip, Fail }

    private sealed record ScenarioResult(string Name, ScenarioStatus Status, int? Expected, int? Actual, string? Detail);
}

file static class ValidationDtoExtensions
{
    public static UpsertLocaleRequest ToRequest(this LocaleConfigurationDto value,
        string? dateFormat = null, string? shortDateFormat = null, string? timeFormat = null,
        string? dateTimeFormat = null, string? firstDayOfWeek = null, string? decimalSeparator = null,
        string? thousandsSeparator = null, int? decimalPlaces = null, int? currencyDecimalPlaces = null)
        => new(
            dateFormat ?? value.DateFormat,
            shortDateFormat ?? value.ShortDateFormat,
            timeFormat ?? value.TimeFormat,
            dateTimeFormat ?? value.DateTimeFormat,
            firstDayOfWeek ?? value.FirstDayOfWeek,
            decimalSeparator ?? value.DecimalSeparator,
            thousandsSeparator ?? value.ThousandsSeparator,
            decimalPlaces ?? value.DecimalPlaces,
            currencyDecimalPlaces ?? value.CurrencyDecimalPlaces);

    public static UpsertCurrencyRequest ToRequest(this CurrencyFormatDto value,
        string? currencyName = null, string? symbol = null, string? symbolPosition = null,
        bool? spaceBetweenSymbolAndAmount = null, string? decimalSeparator = null,
        string? thousandsSeparator = null, int? decimalPlaces = null, string? negativePattern = null)
        => new(
            currencyName ?? value.CurrencyName,
            symbol ?? value.Symbol,
            symbolPosition ?? value.SymbolPosition,
            spaceBetweenSymbolAndAmount ?? value.SpaceBetweenSymbolAndAmount,
            decimalSeparator ?? value.DecimalSeparator,
            thousandsSeparator ?? value.ThousandsSeparator,
            decimalPlaces ?? value.DecimalPlaces,
            negativePattern ?? value.NegativePattern);
}
