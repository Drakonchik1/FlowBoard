namespace FlowBoard.Application.Common.Interfaces;

/// <summary>
/// Exposes the authenticated user's identity to Application handlers.
/// Reads claims from the HTTP context JWT — implemented in API layer via IHttpContextAccessor.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>The authenticated user's ID, or null if the request is anonymous.</summary>
    Guid? UserId { get; }

    /// <summary>Returns true when a valid JWT is present and the UserId claim is set.</summary>
    bool IsAuthenticated { get; }
}
