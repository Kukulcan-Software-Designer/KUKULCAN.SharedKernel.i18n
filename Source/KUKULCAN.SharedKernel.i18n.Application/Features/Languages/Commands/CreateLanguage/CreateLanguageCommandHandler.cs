using KUKULCAN.SharedKernel.i18n.Domain.DTOs;
using KUKULCAN.SharedKernel.i18n.Domain.Interfaces.Repositories;
using KUKULCAN.SharedKernel.i18n.Application.Common;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Queries.GetAllLanguages;
using KUKULCAN.SharedKernel.Identifiers.Interfaces;

namespace KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Commands.CreateLanguage;

/// <summary>
/// Encapsula el manejo de la creación de un nuevo idioma, gestionando la persistencia, la validación de duplicados y la
/// invalidación de cachés relacionados.
/// </summary>
/// <remarks>Esta clase implementa el patrón Command Handler para la creación de idiomas y asegura la coherencia
/// de los datos y la caché. No es segura para subprocesos concurrentes.</remarks>
/// <param name="repository">El repositorio utilizado para acceder y almacenar entidades de idioma.</param>
/// <param name="unitOfWork">La unidad de trabajo responsable de confirmar los cambios en la base de datos.</param>
/// <param name="cache">El servicio de caché empleado para invalidar las entradas de caché de idiomas tras la creación.</param>
public sealed class CreateLanguageCommandHandler(ILanguageRepository repository, IUnitOfWork unitOfWork, ICacheService cache) : IRequestHandler<CreateLanguageCommand, Result<LanguageDto>>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">The request parameter.</param>
    /// <param name="cancellationToken">The cancellationToken parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<Result<LanguageDto>> Handle(CreateLanguageCommand request, CancellationToken cancellationToken)
    {
        if (await repository.ExistsByCodeAsync(request.Code, cancellationToken))
            return Result<LanguageDto>.Failure(I18nErrors.Conflict("Language.Duplicate", $"Language '{request.Code}' already exists."));

        var createResult = Language.Create(Guid.CreateVersion7(), request.Code, request.Name,
            request.NativeName, request.IsDefault);

        if (createResult.IsFailure)
            return Result<LanguageDto>.Failure(createResult.Error);

        await repository.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await InvalidateLanguageCachesAsync(cancellationToken);

        return Result<LanguageDto>.Success(GetAllLanguagesQueryHandler.MapToDto(createResult.Value));
    }

    private async Task InvalidateLanguageCachesAsync(CancellationToken ct)
    {
        await cache.RemoveAsync(I18NCacheKeys.LanguagesAll, ct);
        await cache.RemoveAsync(I18NCacheKeys.LanguagesActive, ct);
    }
}
