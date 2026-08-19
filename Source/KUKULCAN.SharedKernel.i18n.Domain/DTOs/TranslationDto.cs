namespace KUKULCAN.SharedKernel.i18n.Domain.DTOs;

/// <summary>Represents a translated text returned by the application layer.</summary>
public record TranslationDto(
    Guid Id,
    string Code,
    string Module,
    string LanguageCode,
    string Text,
    string? Context,
    int? MaxLength,
    bool IsReviewed,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);
