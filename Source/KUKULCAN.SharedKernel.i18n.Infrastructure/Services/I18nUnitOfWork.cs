using DatabaseUnitOfWork = KUKULCAN.SharedKernel.Database.Abstractions.IUnitOfWork;
using ApplicationUnitOfWork = KUKULCAN.SharedKernel.i18n.Application.Abstractions.IUnitOfWork;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.Services;

/// <summary>Adapts the SharedKernel.Database unit-of-work contract to the i18n application contract.</summary>
public sealed class I18nUnitOfWork(DatabaseUnitOfWork inner) : ApplicationUnitOfWork
{
    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => inner.SaveChangesAsync(cancellationToken);
    /// <inheritdoc />
    public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => inner.BeginTransactionAsync(cancellationToken);
    /// <inheritdoc />
    public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => inner.CommitTransactionAsync(cancellationToken);
    /// <inheritdoc />
    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => inner.RollbackTransactionAsync(cancellationToken);
    /// <inheritdoc />
    public Task EndTransactionAsync(CancellationToken cancellationToken = default) => inner.EndTransactionAsync(cancellationToken);
    /// <inheritdoc />
    public void Dispose() { }
    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
