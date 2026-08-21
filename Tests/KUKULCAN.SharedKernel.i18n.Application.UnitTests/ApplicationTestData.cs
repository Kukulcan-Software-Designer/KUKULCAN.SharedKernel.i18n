using KUKULCAN.SharedKernel.i18n.Domain.ValueObjects.Enums;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests;

internal static class ApplicationTestData
{
    public static Domain.Entities.Language Language(string code = "es-ES", bool isDefault = false, bool active = true)
    {
        var result = Domain.Entities.Language.Create(Guid.CreateVersion7(), code, "Spanish", "Español", isDefault);
        var value = result.Value;
        if (!active && !isDefault) value.Deactivate();
        return value;
    }

    public static Domain.Entities.Translation Translation(string code = "CRM0001", string language = "es-ES", string text = "Hola")
        => Domain.Entities.Translation.Create(Guid.CreateVersion7(), code, language, text, "test", 100).Value;

    public static Domain.Entities.LocaleConfiguration Locale(string language = "es-ES")
        => Domain.Entities.LocaleConfiguration.Create(Guid.CreateVersion7(), language, "dd/MM/yyyy", "dd/MM/yy", "HH:mm", "dd/MM/yyyy HH:mm", FirstDayOfWeek.Monday, ',', '.', 2, 2).Value;
}
