namespace FlowBoard.Domain.Events;

/// <summary>Raised when a new user account is created.</summary>
public sealed record UserRegisteredEvent(Guid UserId, string Email) : IDomainEvent;
