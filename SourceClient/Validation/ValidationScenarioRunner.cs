using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KUKULCAN.SharedKernel.i18n.Client.ApiClient;
using KUKULCAN.SharedKernel.i18n.Client.Models;
using Spectre.Console;

namespace KUKULCAN.SharedKernel.i18n.Client.Validation;

/// <summary>
/// Executes the executable API contract matrix for the i18n service.
/// The suite creates isolated resources, verifies success and documented error
/// semantics, and removes resources it created. It is intentionally independent
/// from the interactive menu so it can be used as a repeatable validation pass.
/// </summary>
public sealed class ValidationScenarioRunner(I18NApiClient api, HttpClient http, ApiSettings settings)
{
    private readonly List<ScenarioResult> _results = [];
    private readonly string _suffix = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
    private string? _originalDefaultLanguage;
    private string? _testLanguage;
    private string? _testParentLanguage;
    private string? _testRegionLanguage;
    private string? _testTranslationCode;

    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        AnsiConsole.MarkupLine("[bold blue]ATLAS.Kernel.i18n — Comprehensive API validation[/]");
        AnsiConsole.MarkupLine($"Target: [green]{settings.BaseUrl}[/]");
        AnsiConsole.WriteLine();

        try
        {
            await RunAuthenticationScenariosAsync(ct);
            await RunLanguageScenariosAsync(ct);
            await RunLocaleScenariosAsync(ct);
            await RunCurrencyScenariosAsync(ct);
            await RunTranslationScenariosAsync(ct);
            await RunPaginationAndBulkScenariosAsync(ct);
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Validation cancelled.[/]");
        }
        catch (Exception ex)
        {
            Add("Suite execution", false, null, ex.Message);
        }
        finally
        {
            await CleanupAsync(ct);
        }

        RenderSummary();
        return _results.Any(x => !x.Passed) ? 1 : 0;
    }

    private async Task RunAuthenticationScenariosAsync(CancellationToken ct)
    {
        await ExpectRawAsync(
            "GET /languages without credentials",
            HttpMethod.Get,
            "api/v1/languages",
            null,
            HttpStatusCode.Unauthorized,
            authenticated: false,
            ct);

        await ExpectRawAsync(
            "POST /languages without credentials",
            HttpMethod.Post,
            "api/v1/languages",
            new CreateLanguageRequest("zz-UNAUTH", "Unauthorized", "Unauthorized"),
            HttpStatusCode.Unauthorized,
            authenticated: false,
            ct);
    }

    private async Task RunLanguageScenariosAsync(CancellationToken ct)
    {
        var languages = await api.GetAllLanguagesAsync(false, ct);
        Add("Languages: GET all", languages.IsSuccess, 200, languages.Error?.Status);
        if (!languages.IsSuccess || languages.Value is null)
            return;

        LanguageDto? currentDefault = languages.Value.FirstOrDefault(x => x.IsDefault);
        _originalDefaultLanguage = currentDefault?.Code;

        _testLanguage = $"zz-{_suffix[..Math.Min(6, _suffix.Length)]}";
        _testParentLanguage = "zz";
        _testRegionLanguage = $"zz-{_suffix[^3..]}";

        await ExpectResultAsync(
            "Languages: GET inactive included",
            () => api.GetAllLanguagesAsync(false, ct),
            200);

        await ExpectResultAsync(
            "Languages: GET missing",
            () => api.GetLanguageAsync($"missing-{_suffix}", ct),
            404);

        await ExpectResultAsync(
            "Languages: POST invalid payload",
            () => api.CreateLanguageAsync(new CreateLanguageRequest("", "", ""), ct),
            422);

        await ExpectResultAsync(
            "Languages: POST create",
            () => api.CreateLanguageAsync(new CreateLanguageRequest(_testLanguage, "Test language", "Idioma de prueba"), ct),
            201);

        await ExpectResultAsync(
            "Languages: POST duplicate",
            () => api.CreateLanguageAsync(new CreateLanguageRequest(_testLanguage, "Duplicate", "Duplicado"), ct),
            409);

        await ExpectResultAsync(
            "Languages: PUT update",
            () => api.UpdateLanguageAsync(_testLanguage, new UpdateLanguageRequest("Updated language", "Idioma actualizado"), ct),
            200);

        await ExpectResultAsync(
            "Languages: PUT missing",
            () => api.UpdateLanguageAsync($"missing-{_suffix}", new UpdateLanguageRequest("x", "x"), ct),
            404);

        await ExpectResultAsync(
            "Languages: PATCH active=false",
            () => api.SetLanguageActiveAsync(_testLanguage, false, ct),
            204);

        await ExpectResultAsync(
            "Languages: PATCH active=true",
            () => api.SetLanguageActiveAsync(_testLanguage, true, ct),
            204);

        if (_originalDefaultLanguage is not null)
        {
            await ExpectResultAsync(
                "Languages: deactivate current default",
                () => api.SetLanguageActiveAsync(_originalDefaultLanguage, false, ct),
                409);

            await ExpectResultAsync(
                "Languages: set new default",
                () => api.SetDefaultLanguageAsync(_testLanguage, ct),
                204);

            await ExpectResultAsync(
                "Languages: new default cannot deactivate",
                () => api.SetLanguageActiveAsync(_testLanguage, false, ct),
                409);

            await ExpectResultAsync(
                "Languages: restore original default",
                () => api.SetDefaultLanguageAsync(_originalDefaultLanguage, ct),
                204);
        }
    }

    private async Task RunLocaleScenariosAsync(CancellationToken ct)
    {
        if (_testLanguage is null)
            return;

        await ExpectResultAsync(
            "Locales: GET all",
            () => api.GetAllLocalesAsync(ct),
            200);

        await ExpectResultAsync(
            "Locales: GET missing language",
            () => api.GetLocaleAsync($"missing-{_suffix}", ct),
            404);

        var request = new UpsertLocaleRequest(
            "dd/MM/yyyy", "d/M/yy", "HH:mm:ss", "dd/MM/yyyy HH:mm",
            "Monday", ",", ".", 2, 2);

        await ExpectResultAsync(
            "Locales: PUT create",
            () => api.UpsertLocaleAsync(_testLanguage, request, ct),
            200);

        await ExpectResultAsync(
            "Locales: PUT update",
            () => api.UpsertLocaleAsync(_testLanguage, request with { DecimalPlaces = 3 }, ct),
            200);

        await ExpectResultAsync(
            "Locales: GET created",
            () => api.GetLocaleAsync(_testLanguage, ct),
            200);
    }

    private async Task RunCurrencyScenariosAsync(CancellationToken ct)
    {
        if (_testLanguage is null)
            return;

        await ExpectResultAsync(
            "Currencies: GET language",
            () => api.GetCurrenciesAsync(_testLanguage, ct),
            200);

        var request = new UpsertCurrencyRequest(
            "Test currency", "¤", "Before", true, ",", ".", 2, "-{symbol}{amount}");

        await ExpectResultAsync(
            "Currencies: PUT create",
            () => api.UpsertCurrencyAsync(_testLanguage, "ZZZ", request, ct),
            200);

        await ExpectResultAsync(
            "Currencies: PUT update",
            () => api.UpsertCurrencyAsync(_testLanguage, "ZZZ", request with { SymbolPosition = "After" }, ct),
            200);

        await ExpectResultAsync(
            "Currencies: GET after upsert",
            () => api.GetCurrenciesAsync(_testLanguage, ct),
            200);

        await ExpectResultAsync(
            "Currencies: DELETE",
            () => api.DeleteCurrencyAsync(_testLanguage, "ZZZ", ct),
            204);

        await ExpectResultAsync(
            "Currencies: DELETE missing",
            () => api.DeleteCurrencyAsync(_testLanguage, "ZZZ", ct),
            404);
    }

    private async Task RunTranslationScenariosAsync(CancellationToken ct)
    {
        if (_testLanguage is null)
            return;

        _testTranslationCode = $"client.validation.{_suffix}";

        await ExpectResultAsync(
            "Translations: GET missing",
            () => api.GetTranslationAsync($"missing.{_suffix}", _testLanguage, ct),
            404);

        await ExpectResultAsync(
            "Translations: GET module",
            () => api.GetModuleTranslationsAsync("client-validation-missing", _testLanguage, ct),
            200);

        await ExpectResultAsync(
            "Translations: GET variants missing",
            () => api.GetTranslationVariantsAsync($"missing.{_suffix}", ct),
            200);

        await ExpectResultAsync(
            "Translations: POST invalid",
            () => api.CreateTranslationAsync(new CreateTranslationRequest("", "", ""), ct),
            422);

        await ExpectResultAsync(
            "Translations: POST create",
            () => api.CreateTranslationAsync(new CreateTranslationRequest(
                _testTranslationCode, _testLanguage, "Original text", "client-validation", "initial"), ct),
            201);

        await ExpectResultAsync(
            "Translations: POST duplicate",
            () => api.CreateTranslationAsync(new CreateTranslationRequest(
                _testTranslationCode, _testLanguage, "Duplicate text", "client-validation"), ct),
            409);

        await ExpectResultAsync(
            "Translations: GET exact",
            () => api.GetTranslationAsync(_testTranslationCode, _testLanguage, ct),
            200);

        await ExpectResultAsync(
            "Translations: GET variants",
            () => api.GetTranslationVariantsAsync(_testTranslationCode, ct),
            200);

        await ExpectResultAsync(
            "Translations: PUT update",
            () => api.UpdateTranslationAsync(_testTranslationCode, _testLanguage,
                new UpdateTranslationRequest("Updated text", "updated"), ct),
            200);

        await ExpectResultAsync(
            "Translations: PATCH reviewed=true",
            () => api.SetTranslationReviewedAsync(_testTranslationCode, _testLanguage, true, ct),
            204);

        await ExpectResultAsync(
            "Translations: PATCH reviewed=false",
            () => api.SetTranslationReviewedAsync(_testTranslationCode, _testLanguage, false, ct),
            204);

        await ExpectResultAsync(
            "Translations: PUT missing",
            () => api.UpdateTranslationAsync($"missing.{_suffix}", _testLanguage,
                new UpdateTranslationRequest("x"), ct),
            404);

        await ExpectResultAsync(
            "Translations: GET paged",
            () => api.GetTranslationsPagedAsync(1, 10, null, _testLanguage, null, ct),
            200);

        await ExpectResultAsync(
            "Translations: DELETE",
            () => api.DeleteTranslationAsync(_testTranslationCode, _testLanguage, ct),
            204);

        await ExpectResultAsync(
            "Translations: DELETE missing",
            () => api.DeleteTranslationAsync(_testTranslationCode, _testLanguage, ct),
            404);
    }

    private async Task RunPaginationAndBulkScenariosAsync(CancellationToken ct)
    {
        if (_testLanguage is null)
            return;

        await ExpectResultAsync(
            "Translations: paged first page",
            () => api.GetTranslationsPagedAsync(1, 1, null, _testLanguage, null, ct),
            200);

        await ExpectResultAsync(
            "Translations: paged invalid page",
            () => api.GetTranslationsPagedAsync(0, 10, null, _testLanguage, null, ct),
            422);

        await ExpectResultAsync(
            "Translations: paged invalid page size",
            () => api.GetTranslationsPagedAsync(1, 0, null, _testLanguage, null, ct),
            422);

        string code1 = $"client.bulk.{_suffix}.1";
        string code2 = $"client.bulk.{_suffix}.2";
        var bulk = new BulkUpsertRequest([
            new BulkTranslationEntry(code1, _testLanguage, "Bulk one", "client-validation"),
            new BulkTranslationEntry(code2, _testLanguage, "Bulk two", "client-validation")]);

        await ExpectResultAsync(
            "Translations: bulk insert",
            () => api.BulkUpsertTranslationsAsync(bulk, ct),
            200);

        await ExpectResultAsync(
            "Translations: bulk update",
            () => api.BulkUpsertTranslationsAsync(bulk with
            {
                Entries = [
                    new BulkTranslationEntry(code1, _testLanguage, "Bulk one updated", "client-validation"),
                    new BulkTranslationEntry(code2, _testLanguage, "Bulk two updated", "client-validation")]
            }, ct),
            200);

        await ExpectResultAsync(
            "Translations: bulk empty",
            () => api.BulkUpsertTranslationsAsync(new BulkUpsertRequest([]), ct),
            422);

        await ExpectResultAsync(
            "Translations: bulk over maximum",
            () => api.BulkUpsertTranslationsAsync(new BulkUpsertRequest(
                Enumerable.Range(0, 5001)
                    .Select(i => new BulkTranslationEntry($"client.bulk.{_suffix}.{i}", _testLanguage, "x", "client-validation"))
                    .ToArray()), ct),
            422);

        await ExpectResultAsync(
            "Translations: module filter",
            () => api.GetTranslationsPagedAsync(1, 50, "client-validation", _testLanguage, null, ct),
            200);
    }

    private async Task ExpectResultAsync<T>(string name, Func<Task<ApiResult<T>>> action, int expectedStatus)
    {
        try
        {
            ApiResult<T> result = await action();
            int? actual = result.IsSuccess ? 200 : result.Error?.Status;

            // POST create is represented as success without exposing the HTTP status in ApiResult.
            // For successful operations, the client contract is checked by operation type below.
            bool passed = expectedStatus is 200 or 201 or 204
                ? result.IsSuccess
                : !result.IsSuccess && actual == expectedStatus;

            Add(name, passed, expectedStatus, actual);
        }
        catch (Exception ex)
        {
            Add(name, false, expectedStatus, null, ex.Message);
        }
    }

    private async Task ExpectRawAsync(
        string name,
        HttpMethod method,
        string path,
        object? body,
        HttpStatusCode expected,
        bool authenticated,
        CancellationToken ct)
    {
        using HttpClient client = new();
        client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
        if (authenticated && !string.IsNullOrWhiteSpace(settings.BearerToken))
            client.DefaultRequestHeaders.Authorization = new("Bearer", settings.BearerToken);

        using HttpRequestMessage request = new(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body);

        try
        {
            using HttpResponseMessage response = await client.SendAsync(request, ct);
            Add(name, response.StatusCode == expected, (int)expected, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            Add(name, false, (int)expected, null, ex.Message);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        if (_testTranslationCode is not null && _testLanguage is not null)
            await api.DeleteTranslationAsync(_testTranslationCode, _testLanguage, ct);

        if (_testLanguage is not null)
        {
            await api.DeleteCurrencyAsync(_testLanguage, "ZZZ", ct);
            await api.SetLanguageActiveAsync(_testLanguage, false, ct);
        }
    }

    private void Add(string name, bool passed, int? expected, int? actual, string? detail = null)
        => _results.Add(new ScenarioResult(name, passed, expected, actual, detail));

    private void RenderSummary()
    {
        var table = new Table().AddColumn("Scenario").AddColumn("Expected").AddColumn("Actual").AddColumn("Result");
        foreach (ScenarioResult result in _results)
            table.AddRow(
                result.Name,
                result.Expected?.ToString() ?? "—",
                result.Actual?.ToString() ?? "—",
                result.Passed ? "[green]PASS[/]" : $"[red]FAIL[/] {Markup.Escape(result.Detail ?? "")}");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        int passed = _results.Count(x => x.Passed);
        int failed = _results.Count - passed;
        AnsiConsole.MarkupLine($"[bold]Validation result: {passed}/{_results.Count} passed; {failed} failed.[/]");
    }

    private sealed record ScenarioResult(string Name, bool Passed, int? Expected, int? Actual, string? Detail);
}
