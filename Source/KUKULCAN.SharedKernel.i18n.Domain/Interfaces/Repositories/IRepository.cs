namespace KUKULCAN.SharedKernel.i18n.Domain.Interfaces.Repositories;

/// <summary>Defines the common write repository operations used by the i18n module.</summary>
/// <typeparam name="T">Entity type.</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>Gets an entity by its public GUID identifier.</summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Lists all entities.</summary>
    Task<IReadOnlyList<T>> ListAllAsync(CancellationToken cancellationToken = default);
    /// <summary>Adds an entity.</summary>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    /// <summary>Marks an entity as modified.</summary>
    void Update(T entity);
    /// <summary>Determines whether an entity exists by identifier.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
