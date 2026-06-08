namespace FlowBoard.Domain.Exceptions;

/// <summary>Thrown when a requested resource does not exist or has been soft-deleted.</summary>
public sealed class NotFoundException(string resourceName, object id)
    : Exception($"{resourceName} with id '{id}' was not found.");
