using KUKULCAN.SharedKernel.Identifiers;

namespace KUKULCAN.SharedKernel.i18n.Domain.Identifiers;

/// <summary>
/// Strongly typed GUID identifier used by the i18n domain entities.
/// </summary>
public sealed class I18nEntityId : GuidEntityId
{
    /// <summary>
    /// Initializes an identifier for EF Core materialization.
    /// </summary>
    public I18nEntityId()
    {
    }

    /// <summary>
    /// Initializes an identifier from a GUID value.
    /// </summary>
    /// <param name="value">The underlying GUID.</param>
    public I18nEntityId(Guid value) : base(value)
    {
    }

    /// <summary>
    /// Converts a GUID to an i18n entity identifier.
    /// </summary>
    public static implicit operator I18nEntityId(Guid value) => new(value);

    /// <summary>
    /// Converts an i18n entity identifier to its underlying GUID.
    /// </summary>
    public static implicit operator Guid(I18nEntityId value) => value.Value;

    /// <summary>
    /// Compares two i18n entity identifiers.
    /// </summary>
    public static bool operator ==(I18nEntityId? left, I18nEntityId? right) => Equals(left, right);

    /// <summary>
    /// Compares two i18n entity identifiers for inequality.
    /// </summary>
    public static bool operator !=(I18nEntityId? left, I18nEntityId? right) => !Equals(left, right);

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is I18nEntityId other && Value == other.Value;

    /// <inheritdoc />
    public override int GetHashCode()
        => Value.GetHashCode();
}
