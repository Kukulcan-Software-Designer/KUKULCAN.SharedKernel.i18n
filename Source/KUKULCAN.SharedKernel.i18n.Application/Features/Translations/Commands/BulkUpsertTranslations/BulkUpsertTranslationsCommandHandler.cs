using KUKULCAN.SharedKernel.i18n.Application.Common;
using KUKULCAN.SharedKernel.i18n.Domain.DTOs;
using KUKULCAN.SharedKernel.i18n.Domain.Interfaces.Repositories;
using KUKULCAN.SharedKernel.Identifiers.Interfaces;

namespace KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Commands.BulkUpsertTranslations;

/// <summary>
/// Represents the BulkUpsertTranslationsCommandHandler type.
/// </summary>
/// <param name="translationRepo">The translationRepo parameter.</param>
/// <param name="languageRepo">The languageRepo parameter.</param>
/// <param name="unitOfWork">The unitOfWork parameter.</param>
/// <param name="cache">The cache parameter.</param>
public sealed class BulkUpsertTranslationsCommandHandler(ITranslationRepository translationRepo, ILanguageRepository languageRepo, IUnitOfWork unitOfWork,
    ICacheService cache): IRequestHandler<BulkUpsertTranslationsCommand, Result<BulkUpsertResultDto>>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">The request parameter.</param>
    /// <param name="cancellationToken">The cancellationToken parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<Result<BulkUpsertResultDto>> Handle(BulkUpsertTranslationsCommand request, CancellationToken cancellationToken)
    {
        var created = 0;
        var updated = 0;
        var errors = new List<string>();
        var moduleLangPairs = new HashSet<(string, string)>();

        // Pre-load active language codes
        var activeLangs = (await languageRepo.GetAllActiveAsync(cancellationToken))
            .Select(l => l.Code.ToLowerInvariant())
            .ToHashSet();

        foreach (var item in request.Items)
        {
            var codeResult = TranslationCode.From(item.Code);
            if (codeResult.IsFailure)
            {
                errors.Add($"{item.Code}: {codeResult.Error.Description}");
                continue;
            }

            var langResult = LanguageCode.Create(item.LanguageCode);

            if (langResult.IsFailure)
            {
                errors.Add($"{item.Code}/{item.LanguageCode}: {langResult.Error.Description}");
                continue;
            }

            var code = codeResult.Value;
            var lang = langResult.Value;

            if (!activeLangs.Contains(lang.Value.ToLowerInvariant()))
            {
                errors.Add($"{item.Code}/{lang.Value}: Language not found or inactive.");
                continue;
            }

            var existing = await translationRepo.FindAsync(code, lang, cancellationToken);

            if (existing is null)
            {
                var createResult = Translation.Create(
                    Guid.CreateVersion7(),
                    item.Code, item.LanguageCode, item.Text, item.Context, item.MaxLength);

                if (createResult.IsFailure)
                {
                    errors.Add($"{item.Code}: {createResult.Error.Description}");
                    continue;
                }
                await translationRepo.AddAsync(createResult.Value, cancellationToken);
                created++;
            }
            else
            {
                var updateResult = existing.UpdateText(item.Text);

                if (updateResult.IsFailure)
                {
                    errors.Add($"{item.Code}: {updateResult.Error.Description}");
                    continue;
                }
                existing.UpdateContext(item.Context);
                translationRepo.Update(existing);
                updated++;
            }
            moduleLangPairs.Add((code.Module, lang.Value));
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        // Invalidate module caches
        foreach (var (module, lang) in moduleLangPairs)
            await cache.RemoveAsync(I18NCacheKeys.ModuleTranslations(module, lang), cancellationToken);

        return Result<BulkUpsertResultDto>.Success(new BulkUpsertResultDto(created, updated, errors));
    }
}
