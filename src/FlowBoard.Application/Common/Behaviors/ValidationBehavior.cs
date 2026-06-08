using FluentValidation;
using MediatR;
using AppValidationException = FlowBoard.Application.Common.Exceptions.ValidationException;
using AppValidationError = FlowBoard.Application.Common.Exceptions.ValidationError;

namespace FlowBoard.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs all registered FluentValidation validators for the request.
/// If any validator fails, throws ValidationException before the handler is ever invoked.
/// Runs after LoggingBehavior and before CachingBehavior.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => new AppValidationError(f.PropertyName, f.ErrorMessage))
            .ToList();

        if (errors.Count != 0)
            throw new AppValidationException(errors);

        return await next(cancellationToken);
    }
}
