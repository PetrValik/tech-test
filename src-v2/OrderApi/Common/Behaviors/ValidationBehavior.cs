using FluentValidation;
using MediatR;

namespace OrderApi.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs FluentValidation validators before the handler executes.
/// If validation fails, throws a <see cref="ValidationException"/> which the global exception handler
/// converts into a 400 Bad Request with structured error details.
/// </summary>
/// <param name="validators">All registered <see cref="IValidator{T}"/> instances for the request type.</param>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Runs all registered <see cref="IValidator{T}"/> instances for the request.
    /// Throws <see cref="ValidationException"/> if any validation failures are found;
    /// otherwise invokes the next handler in the pipeline.
    /// </summary>
    /// <param name="request">The incoming MediatR request to validate and handle.</param>
    /// <param name="next">The delegate representing the next handler in the pipeline.</param>
    /// <param name="cancellationToken">Token used to cancel validation or the downstream handler.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the response produced by the next handler.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                validators.Select(validator => validator.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
