namespace KUKULCAN.SharedKernel.i18n.Infrastructure.Primitives;

/// <summary>Provides time-ordered GUIDs for persistence identifiers.</summary>
public static class SequentialGuid
{
    /// <summary>Creates a UUID version 7, whose timestamp component provides natural ordering.</summary>
    public static Guid NewSequentialGuidAtEnd() => Guid.CreateVersion7();
}
