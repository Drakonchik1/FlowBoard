namespace FlowBoard.Application.Common.Exceptions;

/// <summary>
/// Thrown when a request conflicts with existing state (duplicate email, slug, membership, etc.).
/// Mapped to HTTP 409 Conflict by the global exception handler.
/// </summary>
public sealed class ConflictException(string message) : Exception(message);
