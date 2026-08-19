using KUKULCAN.SharedKernel.i18n.Domain.DTOs;
using KUKULCAN.SharedKernel.i18n.Domain.Interfaces.Repositories;
using KUKULCAN.SharedKernel.i18n.Application.Common;
using KUKULCAN.SharedKernel.Identifiers.Interfaces;

namespace KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Commands.CreateTranslation;

/// <summary>
/// Represents the CreateTranslationCommandHandler type.
/// </summary>
/// <param name="translationRepo">The translationRepo parameter.</param>
/// <param name="languageRepo">The languageRepo parameter.</param>
/// <param name="unitOfWork">The unitOfWork parameter.</param>
/// <param name="cache">The cache parameter.</param>
public sealed class CreateTranslationCommandHandler(ITranslationRepository translationRepo, ILanguageRepository languageRepo,
    IUnitOfWork unitOfWork, ICacheService cache) : IRequestHandler<CreateTranslationCommand, Result<TranslationDto>>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">The request parameter.</param>
    /// <param name="cancellationToken">The cancellationToken parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<Result<TranslationDto>> Handle(CreateTranslationCommand request, CancellationToken cancellationToken)
    {
        var codeResult = TranslationCode.From(request.Code);
        if (codeResult.IsFailure)
            return Result<TranslationDto>.Failure(codeResult.Error);

        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure)
            return Result<TranslationDto>.Failure(langResult.Error);

        var code = codeResult.Value;
        var lang = langResult.Value;

        // Verify language exists and is active
        var language = await languageRepo.GetByCodeAsync(lang.Value, cancellationToken);
        if (language is null)
            return Result<TranslationDto>.Failure(I18nErrors.NotFound("Language.NotFound", $"Language '{lang.Value}' was not found."));

        if (!language.IsActive)
            return Result<TranslationDto>.Failure(I18nErrors.Conflict("Language.Inactive", $"Language '{lang.Value}' is inactive. Translations cannot be added to inactive languages."));

        // Enforce uniqueness (code + language)
        if (await translationRepo.ExistsAsync(code, lang, cancellationToken))
            return Result<TranslationDto>.Failure(I18nErrors.Conflict("Translation.Duplicate", $"A translation for '{code.Value}' in language '{lang.Value}' already exists."));

        // Create entity using Result pattern — use SequentialGuid for PostgreSQL optimisation
        var createResult = Translation.Create(Guid.CreateVersion7(), request.Code, request.LanguageCode, request.Text,
            request.Context, request.MaxLength);

        if (createResult.IsFailure)
            return Result<TranslationDto>.Failure(createResult.Error);

        await translationRepo.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        // Invalidate cache
        await cache.RemoveAsync(I18NCacheKeys.Translation(code.Value, lang.Value), cancellationToken);
        await cache.RemoveAsync(I18NCacheKeys.ModuleTranslations(code.Module, lang.Value), cancellationToken);

        return Result<TranslationDto>.Success(MapToDto(createResult.Value));
    }

    internal static TranslationDto MapToDto(Translation t) =>
        new(t.Id, t.Code.Value, t.Code.Module, t.LanguageCode.Value,
            t.Text, t.Context, t.MaxLength, t.IsReviewed,
            t.CreatedOn, t.ModifiedOn);
}

