namespace FlowBoard.Application.Common.Exceptions;

/// <summary>Thrown when authentication fails (bad credentials, expired/revoked token, family reuse detected).</summary>
public sealed class UnauthorizedException(string message) : Exception(message);
