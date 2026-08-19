namespace KUKULCAN.SharedKernel.i18n.Domain.DTOs;

/// <summary>Represents a language returned by the application layer.</summary>
public record LanguageDto(
    Guid Id,
    string Code,
    string Name,
    string NativeName,
    bool IsDefault,
    bool IsActive,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);
