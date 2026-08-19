using KUKULCAN.SharedKernel.DomainEvents.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.Services;

/// <summary>
/// Dispatches SharedKernel domain events to registered domain-event handlers.
/// </summary>
/// <param name="serviceProvider">The dependency-injection service provider.</param>
public sealed class I18NDomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    /// <inheritdoc />
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        Type handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        IEnumerable<object?> handlers = serviceProvider.GetServices(handlerType);

        foreach (object? handler in handlers)
        {
            if (handler is null)
                continue;

            var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync));
            if (method is null)
                continue;

            if (method.Invoke(handler, [domainEvent, cancellationToken]) is Task task)
                await task.ConfigureAwait(false);
        }
    }
}
