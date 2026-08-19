using FluentValidation;
using KUKULCAN.SharedKernel.Results;
using KUKULCAN.SharedKernel.Validations;
using MediatR;

namespace KUKULCAN.SharedKernel.i18n.Application.Behaviors;

/// <summary>Executes registered FluentValidation validators before request handlers.</summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        IValidator<TRequest>[] registered = validators.ToArray();
        if (registered.Length == 0)
            return await next(cancellationToken);

        ValidationContext<TRequest> context = new(request);
        FluentValidation.Results.ValidationResult[] results = await Task.WhenAll(
            registered.Select(v => v.ValidateAsync(context, cancellationToken)));
        List<FluentValidation.Results.ValidationFailure> failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next(cancellationToken);

        if (typeof(TResponse) == typeof(Result))
        {
            ValidationResult validationResult = ValidationResult.Failure(
                failures.Select(f => new ValidationFailure(
                    f.PropertyName,
                    f.ErrorCode,
                    f.ErrorMessage,
                    f.AttemptedValue)));
            return (TResponse)(object)validationResult.ToResult();
        }

        if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            Error error = new("Validation.Failed", $"Validation failed with {failures.Count} error(s).");
            return (TResponse)typeof(TResponse)
                .GetMethod(nameof(Result<object>.Failure), [typeof(Error)])!
                .Invoke(null, [error])!;
        }

        throw new ValidationException(failures);
    }
}
