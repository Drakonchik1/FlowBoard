namespace FlowBoard.Domain.Events;

/// <summary>
/// Marker interface for all domain events. Zero external dependencies — Domain is pure C#.
/// In Infrastructure, UnitOfWork wraps each event in a MediatR notification for dispatching.
/// </summary>
public interface IDomainEvent;
