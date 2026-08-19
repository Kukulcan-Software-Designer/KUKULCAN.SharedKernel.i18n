namespace KUKULCAN.SharedKernel.i18n.Application.Abstractions;

/// <summary>Marks a MediatR request as eligible for response caching.</summary>
public interface ICacheableRequest
{
    /// <summary>Gets the cache key.</summary>
    string CacheKey { get; }
    /// <summary>Gets the cache duration.</summary>
    TimeSpan? CacheDuration { get; }
}
