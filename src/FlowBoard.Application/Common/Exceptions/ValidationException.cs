namespace FlowBoard.Application.Common.Exceptions;

/// <summary>
/// Thrown by ValidationBehavior when FluentValidation rules fail.
/// Carries all validation errors so the exception handler can return a 422 Problem Details response.
/// </summary>
public sealed class ValidationException(IEnumerable<ValidationError> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyList<ValidationError> Errors { get; } = errors.ToList();
}

public sealed record ValidationError(string PropertyName, string ErrorMessage);
