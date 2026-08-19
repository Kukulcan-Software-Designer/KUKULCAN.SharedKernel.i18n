using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KUKULCAN.SharedKernel.i18n.Application.Behaviors;

/// <summary>Logs the execution time and outcome of MediatR requests.</summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Gets or sets the threshold above which a request is logged as slow.</summary>
    public static int SlowRequestThresholdMs { get; set; } = 500;

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        string name = typeof(TRequest).Name;
        logger.LogInformation("[Pipeline] Handling {RequestName}", name);
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            TResponse response = await next(cancellationToken);
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMs)
                logger.LogWarning("[Pipeline] Slow request — {RequestName} took {ElapsedMs}ms", name, stopwatch.ElapsedMilliseconds);
            else
                logger.LogInformation("[Pipeline] {RequestName} completed in {ElapsedMs}ms", name, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            logger.LogError(exception, "[Pipeline] {RequestName} failed after {ElapsedMs}ms", name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
