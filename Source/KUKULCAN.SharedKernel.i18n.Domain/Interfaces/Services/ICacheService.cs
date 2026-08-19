namespace KUKULCAN.SharedKernel.i18n.Domain.Interfaces.Services;

/// <summary>Provides asynchronous cache access for the i18n module.</summary>
public interface ICacheService
{
    /// <summary>Gets a cached value.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    /// <summary>Stores a value in the cache.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    /// <summary>Removes a cache entry.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>Gets a cached value or creates it through the factory.</summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    /// <summary>Determines whether a cache entry exists.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>Removes entries matching a prefix when supported by the implementation.</summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
