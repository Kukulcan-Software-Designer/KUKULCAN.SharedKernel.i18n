using KUKULCAN.SharedKernel.i18n.Application.Abstractions;
using KUKULCAN.SharedKernel.i18n.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KUKULCAN.SharedKernel.i18n.Application.Behaviors;

/// <summary>Provides cache-aside behavior for requests marked with <see cref="ICacheableRequest"/>.</summary>
public sealed class CachingBehavior<TRequest, TResponse>(ICacheService cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ICacheableRequest cacheable)
            return await next(cancellationToken);

        TResponse? cached = await cache.GetAsync<TResponse>(cacheable.CacheKey, cancellationToken);
        if (cached is not null)
        {
            logger.LogDebug("[Cache] HIT — key: {CacheKey}", cacheable.CacheKey);
            return cached;
        }

        logger.LogDebug("[Cache] MISS — key: {CacheKey}", cacheable.CacheKey);
        TResponse response = await next(cancellationToken);
        await cache.SetAsync(cacheable.CacheKey, response, cacheable.CacheDuration, cancellationToken);
        return response;
    }
}
