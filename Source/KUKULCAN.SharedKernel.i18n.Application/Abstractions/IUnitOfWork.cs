namespace KUKULCAN.SharedKernel.i18n.Application.Abstractions;

/// <summary>Defines the persistence operations required by the i18n application layer.</summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>Persists pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    /// <summary>Begins a database transaction.</summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    /// <summary>Commits the current transaction.</summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    /// <summary>Rolls back the current transaction.</summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    /// <summary>Ends the current transaction.</summary>
    Task EndTransactionAsync(CancellationToken cancellationToken = default);
}
