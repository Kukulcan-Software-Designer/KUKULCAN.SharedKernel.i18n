using KUKULCAN.SharedKernel.Database.Abstractions;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.Services;

/// <summary>Provides the system tenant identifier for global i18n data.</summary>
public sealed class I18NSystemTenantContext : ITenantContext
{
    /// <inheritdoc />
    public Guid TenantId => Guid.Empty;
}
