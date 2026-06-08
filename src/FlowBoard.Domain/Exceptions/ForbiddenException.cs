namespace FlowBoard.Domain.Exceptions;

/// <summary>
/// Thrown when an authenticated user is not authorized to perform an operation
/// on a specific resource (e.g., not a member of the workspace, not an Admin).
/// Translated to HTTP 403 by the global exception handler.
/// </summary>
public sealed class ForbiddenException(string message) : Exception(message);